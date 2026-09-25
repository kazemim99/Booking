using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AsanRezerve.Core.Application.Services.Notifications;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.Host.IntegrationTests.Infrastructure.Fakes;
using AsanRezerve.ServiceCatalog.API.Models.Requests;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using AsanRezerve.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// Shared arrange for the review API tests: a booking the customer can actually review, and sign-in as each of
/// the parties a review involves.
/// </summary>
public abstract class ReviewTestBase : ServiceCatalogIntegrationTestBase
{
    protected ReviewTestBase(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    protected sealed record Visit(Guid BookingId, Guid CustomerId, Provider Provider);

    /// <summary>
    /// A booking taken through the real complete endpoint. The salon path (<c>CreateConfirmedByProvider</c>) is the
    /// only way to a completable booking: <c>Complete()</c> is legal only within fifteen minutes of the start and
    /// <c>Confirm()</c> needs two hours' notice.
    /// </summary>
    protected async Task<Visit> CompletedVisitAsync(Provider? provider = null, Guid? customer = null)
    {
        var (bookingId, customerId, owner) = await BookingAsync(provider, customer: customer);

        AuthenticateAsProviderOwner(owner);
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/bookings/{bookingId}/complete",
            new CompleteBookingRequest { CompletionNotes = "انجام شد" });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        ClearAuthenticationHeader();

        return new Visit(bookingId, customerId, owner);
    }

    /// <summary>A confirmed booking that has NOT been completed.</summary>
    protected async Task<Visit> UncompletedVisitAsync()
    {
        var (bookingId, customerId, owner) = await BookingAsync(null);
        return new Visit(bookingId, customerId, owner);
    }

    /// <summary>A confirmed booking still ahead on the salon's clock.</summary>
    protected async Task<Visit> UpcomingVisitAsync()
    {
        var (bookingId, customerId, owner) = await BookingAsync(null, SalonTime.Now.AddDays(2));
        return new Visit(bookingId, customerId, owner);
    }

    /// <summary>A confirmed booking whose time is over, which the salon has not marked done.</summary>
    protected async Task<Visit> PastUncompletedVisitAsync()
    {
        var (bookingId, customerId, owner) = await BookingAsync(null, SalonTime.Now.AddHours(-3));
        return new Visit(bookingId, customerId, owner);
    }

    private async Task<(Guid, Guid, Provider)> BookingAsync(Provider? provider, DateTime? start = null, Guid? customer = null)
    {
        provider ??= await CreateTestProviderWithServicesAsync();
        var service = (await GetProviderServicesAsync(provider.Id.Value)).First();
        var customerId = customer ?? Guid.NewGuid();

        var booking = Booking.CreateConfirmedByProvider(
            UserId.From(customerId),
            provider.Id,
            service.Id,
            provider.Id.Value,
            start ?? DateTime.UtcNow.AddMinutes(5),
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? BookingPolicy.Default,
            "review test");
        await CreateEntityAsync(booking);
        ClearAuthenticationHeader();
        return (booking.Id.Value, customerId, provider);
    }

    /// <summary>
    /// A booking the salon entered for a mobile number (a walk-in or phone booking) and marked done. Its
    /// <c>CustomerId</c> is the salon's owner, as it is for every salon-entered booking; who it is FOR is the entry in
    /// the salon's client book, found by that number.
    /// </summary>
    protected async Task<Visit> SalonEnteredCompletedVisitAsync(string phone)
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = (await GetProviderServicesAsync(provider.Id.Value)).First();

        var entry = ProviderCustomer.Create(
            provider.Id, "مرتضی", "کاظمی", PhoneNumber.From(phone), null, Domain.Enums.CustomerSource.Manual);
        await CreateEntityAsync(entry);

        var booking = Booking.CreateConfirmedByProvider(
            provider.OwnerId,
            provider.Id,
            service.Id,
            provider.Id.Value,
            DateTime.UtcNow.AddMinutes(5),
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? BookingPolicy.Default,
            "salon-entered review test");
        booking.RecordForProviderCustomer(entry.Id, notifyCustomer: false);
        await CreateEntityAsync(booking);

        AuthenticateAsProviderOwner(provider);
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id.Value}/complete",
            new CompleteBookingRequest { CompletionNotes = "انجام شد" });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        ClearAuthenticationHeader();

        return new Visit(booking.Id.Value, provider.OwnerId.Value, provider);
    }

    /// <summary>A mobile number no other test uses.</summary>
    protected static string NewPhone()
    {
        var digits = new string(Guid.NewGuid().ToString("N").Where(char.IsDigit).ToArray()).PadRight(7, '0');
        return "+98912" + digits[..7];
    }

    /// <summary>Signs a person up the way they really do — an OTP to their mobile — and returns their id.</summary>
    protected async Task<Guid> SignUpAsync(string phone, string? firstName, string? lastName)
    {
        ClearAuthenticationHeader();
        var send = await Client.PostAsJsonAsync("/api/v1/auth/send-verification-code",
            new { phoneNumber = phone, countryCode = "+98" });
        send.StatusCode.Should().Be(HttpStatusCode.OK, await send.Content.ReadAsStringAsync());

        var sms = (FakeSmsNotificationService)Factory.Services.GetRequiredService<ISmsNotificationService>();
        var message = sms.LastMessageTo(PhoneNumber.From(phone).Value) ?? sms.LastMessageTo(phone);
        var code = Regex.Match(message!, @"\d{4,8}").Value;

        var complete = await Client.PostAsJsonAsync("/api/v1/auth/customer/complete-authentication",
            new { phoneNumber = phone, code, firstName, lastName });
        complete.StatusCode.Should().Be(HttpStatusCode.OK, await complete.Content.ReadAsStringAsync());
        ClearAuthenticationHeader();
        return Guid.Parse(JObject.Parse(await complete.Content.ReadAsStringAsync())["data"]!["userId"]!.Value<string>()!);
    }

    /// <summary>This booking as the signed-in customer's own booking list shows it.</summary>
    protected async Task<JToken> MyBookingRowAsync(Guid customerId, Guid bookingId)
    {
        AsCustomer(customerId);
        var response = await Client.GetAsync("/api/v1/bookings/my-bookings?pageSize=50");
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        ClearAuthenticationHeader();
        var data = Data(text);
        var items = (JArray)(data["items"] ?? data);
        return items.Single(i => i["bookingId"]!.Value<string>() == bookingId.ToString());
    }

    /// <summary>This booking's page, as the given person opens it.</summary>
    protected async Task<HttpResponseMessage> BookingPageAsync(Guid personId, Guid bookingId)
    {
        AsCustomer(personId);
        var response = await Client.GetAsync($"/api/v1/bookings/{bookingId}");
        ClearAuthenticationHeader();
        return response;
    }

    protected void AsCustomer(Guid customerId) => AuthenticateAsUser(customerId, $"c{customerId:N}@test.com");

    /// <summary>An administrator carrying exactly one role spelling — how production tokens look (FOLLOW-UPS #46).</summary>
    protected void AsAdmin(string role = "Admin") => AuthenticateAs(new TestUser
    {
        UserId = Guid.NewGuid().ToString(),
        Email = $"{role.ToLowerInvariant()}@nahalkmi.ir",
        Name = role,
        Role = role,
    });

    protected Task<HttpResponseMessage> SubmitAsync(Visit visit, object body)
    {
        AsCustomer(visit.CustomerId);
        return Client.PostAsJsonAsync($"/api/v1/reviews/bookings/{visit.BookingId}", body);
    }

    protected static object AReview(decimal rating = 4.5m) =>
        new { rating, comment = "کار تمیز و دقیقی بود، ممنون از شما" };

    /// <summary>Submits a review for the visit and returns its id.</summary>
    protected async Task<Guid> ReviewedAsync(Visit visit, decimal rating = 4.5m, object? body = null)
    {
        var response = await SubmitAsync(visit, body ?? AReview(rating));
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, text);
        ClearAuthenticationHeader();
        return Guid.Parse(Data(text)["reviewId"]!.Value<string>()!);
    }

    /// <summary>
    /// Reads through a fresh scope. The base class's long-lived <c>DbContext</c> would hand back the entity it
    /// tracked before the API call, not what the API wrote.
    /// </summary>
    protected async Task<T> FreshAsync<T>(Func<ServiceCatalogDbContext, Task<T>> read)
    {
        using var scope = Factory.Services.CreateScope();
        return await read(scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>());
    }

    protected Task<Review> LoadReviewAsync(Guid reviewId) =>
        FreshAsync(db => db.Reviews.AsNoTracking().SingleAsync(r => r.Id == reviewId));

    protected Task<Provider> LoadProviderAsync(ProviderId providerId) =>
        FreshAsync(db => db.Providers.AsNoTracking().SingleAsync(p => p.Id == providerId));

    /// <summary>The response's payload, whether or not the host wrapped it in an envelope.</summary>
    protected static JToken Data(string body)
    {
        var token = JToken.Parse(body);
        return token is JObject o && o["data"] is { } data && data.Type != JTokenType.Null ? data : token;
    }
}

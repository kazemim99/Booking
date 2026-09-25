using System.Net;
using System.Net.Http.Json;
using AsanRezerve.Core.Domain.ValueObjects;
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
    protected async Task<Visit> CompletedVisitAsync(Provider? provider = null)
    {
        var (bookingId, customerId, owner) = await BookingAsync(provider);

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

    private async Task<(Guid, Guid, Provider)> BookingAsync(Provider? provider)
    {
        provider ??= await CreateTestProviderWithServicesAsync();
        var service = (await GetProviderServicesAsync(provider.Id.Value)).First();
        var customerId = Guid.NewGuid();

        var booking = Booking.CreateConfirmedByProvider(
            UserId.From(customerId),
            provider.Id,
            service.Id,
            provider.Id.Value,
            DateTime.UtcNow.AddMinutes(5),
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? BookingPolicy.Default,
            "review test");
        await CreateEntityAsync(booking);
        ClearAuthenticationHeader();
        return (booking.Id.Value, customerId, provider);
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

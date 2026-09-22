using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Booksy.Core.Application.Services.Notifications;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Host.IntegrationTests.Infrastructure.Fakes;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// A salon's booking notification names the customer and the service (QA walkthrough 2026-09-22: every one read
/// «مشتری گرامی», because no booking raise site passed a customer name, and none passed the service either).
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class BookingNotificationNamesTests : ServiceCatalogIntegrationTestBase
{
    public BookingNotificationNamesTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private static DateTime NextWeekdayAtHour(DateTime from, int hour)
    {
        var day = from.Date;
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day.AddHours(hour);
    }

    private async Task<Guid> SignUpAsCustomerAsync(string phone, string? firstName, string? lastName)
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
        return Guid.Parse(JObject.Parse(await complete.Content.ReadAsStringAsync())["data"]!["userId"]!.Value<string>()!);
    }

    private async Task<JObject> ParametersOfAsync(Guid bookingId, NotificationEventCode code)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var json = await db.NotificationOutbox.AsNoTracking()
            .Where(e => e.SubjectId == bookingId && e.EventCode == code)
            .Select(e => e.ParametersJson)
            .SingleAsync();
        return JObject.Parse(json);
    }

    private static string? Param(JObject parameters, string key) =>
        parameters.Properties().FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase))
            ?.Value.Value<string>();

    /// <summary>The created booking's id, from the envelope or the bare body.</summary>
    private static Guid BookingIdOf(string body)
    {
        var root = JObject.Parse(body);
        var data = root["data"] as JObject ?? root;
        return Guid.Parse((data["bookingId"] ?? data["id"])!.Value<string>()!);
    }

    private static string RandomMobile() => $"+98912{Random.Shared.Next(1000000, 9999999)}";

    [Fact]
    public async Task A_customers_booking_request_names_them_and_the_service()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var customer = await SignUpAsCustomerAsync(RandomMobile(), "سارا", "احمدی");
        AuthenticateAsUser(customer, $"{Guid.NewGuid():N}@test.com");

        var response = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(2), 10),
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var bookingId = BookingIdOf(body);

        var parameters = await ParametersOfAsync(bookingId, NotificationEventCode.NewBookingRequest);
        Param(parameters, "customerName").Should().Be("سارا احمدی");
        Param(parameters, "serviceName").Should().Be(service.Name);
    }

    [Fact]
    public async Task A_walk_in_is_named_from_the_salons_own_customer_book()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsProviderOwner(provider);

        var response = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(3), 11),
            walkInFirstName = "مرتضی",
            walkInLastName = "کاظمی",
            walkInPhone = RandomMobile(),
            notifyCustomer = false,
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var bookingId = BookingIdOf(body);

        var parameters = await ParametersOfAsync(bookingId, NotificationEventCode.NewBookingConfirmed);
        Param(parameters, "customerName").Should().Be("مرتضی کاظمی",
            "the aggregate's customer is the owner for a walk-in; the name is the booked person's");
    }

    [Fact]
    public async Task A_placeholder_name_is_never_printed_as_if_it_were_one()
    {
        // A person created by OTP without a name is stored as «مشتری <digits>». «مشتری 9123456789 عزیز» is worse
        // than the neutral fallback, so no name is passed at all.
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var customer = await SignUpAsCustomerAsync(RandomMobile(), null, null);
        AuthenticateAsUser(customer, $"{Guid.NewGuid():N}@test.com");

        var response = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(4), 12),
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var bookingId = BookingIdOf(body);

        var parameters = await ParametersOfAsync(bookingId, NotificationEventCode.NewBookingRequest);
        Param(parameters, "customerName").Should().BeNull();
    }
}

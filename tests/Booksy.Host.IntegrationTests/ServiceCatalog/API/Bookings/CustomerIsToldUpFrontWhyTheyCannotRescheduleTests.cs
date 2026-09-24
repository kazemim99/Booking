using System.Net;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// The 24-hour reschedule rule worked in the QA recording (2026-09-24) — but the customer met it only at the very
/// end, after choosing a slot. Their booking now says up front whether it can be moved and, when it cannot, why, in
/// Persian, so the apps can show «تغییر زمان» disabled with the reason instead of a dead end.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class CustomerIsToldUpFrontWhyTheyCannotRescheduleTests : ServiceCatalogIntegrationTestBase
{
    public CustomerIsToldUpFrontWhyTheyCannotRescheduleTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private async Task<(Guid CustomerId, Booking Booking)> BookingAtAsync(DateTime salonStart)
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var service = (await GetProviderServicesAsync(salon.Id.Value)).First();
        var customerId = Guid.NewGuid();
        var booking = Booking.CreateBookingRequest(
            UserId.From(customerId), salon.Id, service.Id, salon.Id.Value, salonStart,
            service.Duration, service.BasePrice, service.BookingPolicy ?? BookingPolicy.Default, "qa");
        await CreateEntityAsync(booking);
        return (customerId, booking);
    }

    private async Task<JToken> GetAsync(Guid customerId, string url)
    {
        AuthenticateAsUser(customerId, "customer@test.com");
        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var root = JToken.Parse(body);
        return root["data"] ?? root;
    }

    private static string? Reason(JToken row)
    {
        var token = row["rescheduleBlockedReason"];
        return token is null || token.Type == JTokenType.Null ? null : token.Value<string>();
    }

    [Fact]
    public async Task Inside_the_window_the_booking_says_why_it_cannot_be_moved()
    {
        var (customerId, booking) = await BookingAtAsync(SalonTime.Now.AddHours(10));

        var details = await GetAsync(customerId, $"/api/v1/bookings/{booking.Id.Value}");
        var list = await GetAsync(customerId, "/api/v1/bookings/my-bookings");
        var row = ((list["items"] as JArray) ?? (JArray)list).Single(i =>
            Guid.Parse((i["bookingId"] ?? i["id"])!.Value<string>()!) == booking.Id.Value);

        foreach (var reason in new[] { Reason(details), Reason(row) })
            reason.Should().Contain("24").And.Contain("ساعت");
    }

    [Fact]
    public async Task Well_ahead_of_the_window_there_is_no_reason_because_it_can_be_moved()
    {
        var (customerId, booking) = await BookingAtAsync(SalonTime.Now.AddDays(4));

        var details = await GetAsync(customerId, $"/api/v1/bookings/{booking.Id.Value}");

        Reason(details).Should().BeNull();
    }
}

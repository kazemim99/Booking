using System.Net;
using System.Net.Http.Json;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// Request item 9 (openspec/changes/_inline/reviews-and-reschedule-round2 D5): after the salon confirms, the customer
/// may still move the booking until two hours before it (was 24), and the moved booking goes back to the salon for
/// confirmation. The response says so outright — the new booking's id and its status — beside the message apps in the
/// field already parse.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class AMovedBookingWaitsForTheSalonTests : ServiceCatalogIntegrationTestBase
{
    public AMovedBookingWaitsForTheSalonTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task A_confirmed_booking_three_hours_away_can_be_moved_and_the_new_one_waits_for_the_salon()
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var service = (await GetProviderServicesAsync(salon.Id.Value)).First();
        var customerId = Guid.NewGuid();
        var booking = Booking.CreateConfirmedByProvider(
            UserId.From(customerId), salon.Id, service.Id, salon.Id.Value, SalonTime.Now.AddHours(3),
            service.Duration, service.BasePrice, service.BookingPolicy ?? BookingPolicy.Default, "round2");
        await CreateEntityAsync(booking);
        booking.Policy.RescheduleWindowHours.Should().Be(2, "the default window");

        AuthenticateAsUser(customerId, "customer@test.com");
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id.Value}/reschedule",
            new RescheduleBookingRequest { NewStartTime = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(5), 14), Reason = "round2" });

        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        var root = JToken.Parse(text);
        var payload = root is JObject o && o["data"] is JObject data ? data : root;
        var newId = Guid.Parse(payload["newBookingId"]!.Value<string>()!);
        payload["status"]!.Value<string>().Should().Be("Requested");
        payload["message"]!.Value<string>().Should().Be($"Booking rescheduled successfully. New booking ID: {newId}",
            "apps in the field parse the id out of it");

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context.ServiceCatalogDbContext>();
        (await db.Bookings.AsNoTracking().SingleAsync(b => b.Id == booking.Id)).Status.Should().Be(BookingStatus.Rescheduled);
        (await db.Bookings.AsNoTracking().SingleAsync(b => b.Id == BookingId.From(newId))).Status
            .Should().Be(BookingStatus.Requested, "the salon confirms the new time again");
    }

    private static DateTime NextWeekdayAtHour(DateTime from, int hour)
    {
        var day = from.Date;
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day.AddHours(hour);
    }
}

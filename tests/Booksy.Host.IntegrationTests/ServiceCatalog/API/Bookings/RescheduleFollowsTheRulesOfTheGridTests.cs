using System.Net;
using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// Moving a booking obeys the same rules as making one — and says so in Persian, in the order the customer can act on
/// (QA 2026-09-24, openspec/changes/_inline/qa-walkthrough-2026-09-25). The screen offered 12:30, and the server then
/// refused it with «The requested time slot is not available»: the free-time grid and booking creation keep no gap
/// after an appointment (since 2026-09-22), but reschedule still kept fifteen minutes. And the 24-hour rule was checked
/// after slot availability, so a customer inside the window first saw a slot error that hid the real reason.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class RescheduleFollowsTheRulesOfTheGridTests : ServiceCatalogIntegrationTestBase
{
    public RescheduleFollowsTheRulesOfTheGridTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private async Task<Booking> RequestAsync(
        Domain.Aggregates.Provider salon, Domain.Aggregates.Service service, Guid customerId, DateTime salonStart)
    {
        var booking = Booking.CreateBookingRequest(
            UserId.From(customerId), salon.Id, service.Id, salon.Id.Value, salonStart,
            service.Duration, service.BasePrice, service.BookingPolicy ?? BookingPolicy.Default, "qa 2026-09-25");
        await CreateEntityAsync(booking);
        return booking;
    }

    private Task<ApiResponse<BookingMessagePayload>> MoveAsync(Guid customerId, Booking booking, DateTime to)
    {
        AuthenticateAsUser(customerId, "customer@test.com");
        return PostAsJsonAsync<RescheduleBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{booking.Id.Value}/reschedule",
            new RescheduleBookingRequest { NewStartTime = to, Reason = "qa" });
    }

    private static string MessageOf(ApiResponse<BookingMessagePayload> response) => response.Error?.Message ?? "";

    [Fact]
    public async Task A_slot_that_ends_exactly_when_the_next_appointment_starts_is_accepted()
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var service = (await GetProviderServicesAsync(salon.Id.Value)).First();
        var day = SalonTime.Now.Date.AddDays(5);
        var taken = day.AddHours(13);
        await RequestAsync(salon, service, Guid.NewGuid(), taken);

        var customerId = Guid.NewGuid();
        var mine = await RequestAsync(salon, service, customerId, day.AddHours(10));

        // The grid offers it: it starts the moment mine ends where the other one begins.
        var response = await MoveAsync(customerId, mine, taken.AddMinutes(-service.Duration.Value));

        response.StatusCode.Should().Be(HttpStatusCode.OK, MessageOf(response));
    }

    [Fact]
    public async Task A_slot_that_really_overlaps_is_refused_in_persian()
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var service = (await GetProviderServicesAsync(salon.Id.Value)).First();
        var day = SalonTime.Now.Date.AddDays(5);
        await RequestAsync(salon, service, Guid.NewGuid(), day.AddHours(13));

        var customerId = Guid.NewGuid();
        var mine = await RequestAsync(salon, service, customerId, day.AddHours(10));
        var response = await MoveAsync(customerId, mine, day.AddHours(13).AddMinutes(5));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, MessageOf(response));
        MessageOf(response).Should().Contain("این زمان").And.NotContain("time slot");
    }

    [Fact]
    public async Task Inside_the_reschedule_window_the_customer_is_told_the_window_not_that_the_slot_is_taken()
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var service = (await GetProviderServicesAsync(salon.Id.Value)).First();
        var day = SalonTime.Now.Date.AddDays(5);
        var wanted = day.AddHours(13);
        await RequestAsync(salon, service, Guid.NewGuid(), wanted); // the wanted slot is ALSO taken

        var customerId = Guid.NewGuid();
        var mine = await RequestAsync(salon, service, customerId, SalonTime.Now.AddHours(10));
        var response = await MoveAsync(customerId, mine, wanted);

        var message = MessageOf(response);
        message.Should().Contain("24").And.Contain("ساعت").And.NotContain("Rescheduling");
        message.Should().NotContain("این زمان", "the window is the reason, not the slot");
    }
}

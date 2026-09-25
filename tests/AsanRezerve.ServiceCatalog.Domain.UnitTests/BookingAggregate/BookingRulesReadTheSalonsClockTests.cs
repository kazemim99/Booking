using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.BookingAggregate;

/// <summary>
/// A booking's time is the salon's wall clock, so every rule that asks "how long until / since the appointment?"
/// must measure against the salon's clock (<see cref="SalonTime"/>). They measured against <c>DateTime.UtcNow</c>,
/// which in Iran is 3:30 behind: a salon could not mark a 10:00 appointment done at 10:33 (QA 2026-09-24,
/// openspec/changes/_inline/qa-walkthrough-2026-09-24), and every window ran three and a half hours off.
/// </summary>
public class BookingRulesReadTheSalonsClockTests
{
    private static Booking ConfirmedAt(DateTime salonStart, int minutes = 60, BookingPolicy? policy = null) =>
        Booking.CreateConfirmedByProvider(
            UserId.From(Guid.NewGuid()),
            ProviderId.New(),
            ServiceId.New(),
            Guid.NewGuid(),
            salonStart,
            Duration.FromMinutes(minutes),
            Price.Create(100, PlatformCurrency.Code),
            policy ?? BookingPolicy.Default);

    [Fact]
    public void The_salon_can_mark_an_appointment_done_half_an_hour_after_it_started()
    {
        var booking = ConfirmedAt(SalonTime.Now.AddMinutes(-33));

        booking.Complete();

        Assert.Equal(BookingStatus.Completed, booking.Status);
    }

    [Fact]
    public void The_salon_still_cannot_mark_it_done_well_before_it_starts()
    {
        var booking = ConfirmedAt(SalonTime.Now.AddMinutes(20));

        Assert.Throws<BusinessRuleViolationException>(() => booking.Complete());
    }

    [Fact]
    public void A_no_show_can_be_recorded_once_the_appointment_has_ended()
    {
        var booking = ConfirmedAt(SalonTime.Now.AddHours(-2), minutes: 60);

        booking.MarkAsNoShow();

        Assert.Equal(BookingStatus.NoShow, booking.Status);
    }

    [Fact]
    public void An_appointment_that_ended_an_hour_ago_is_past()
    {
        var booking = ConfirmedAt(SalonTime.Now.AddHours(-2), minutes: 60);

        Assert.True(booking.IsInPast());
    }

    [Fact]
    public void An_appointment_twenty_two_hours_away_is_within_the_next_day()
    {
        var booking = ConfirmedAt(SalonTime.Now.AddHours(22));

        Assert.True(booking.IsUpcoming());
    }

    [Fact]
    public void An_appointment_inside_the_reschedule_window_cannot_be_moved()
    {
        // Default policy: rescheduling closes 2 hours before the appointment. One hour ahead on the salon's clock is
        // four and a half on UTC's, so only the salon's clock refuses it.
        var booking = ConfirmedAt(SalonTime.Now.AddHours(1));

        Assert.Throws<BusinessRuleViolationException>(
            () => booking.Reschedule(SalonTime.Now.AddDays(3), booking.StaffId));
    }

    [Fact]
    public void A_confirmed_appointment_three_hours_away_can_be_moved_and_waits_for_the_salon_again()
    {
        // openspec/changes/_inline/reviews-and-reschedule-round2 D5: movable until two hours before; the new time is a
        // request the salon confirms again.
        var booking = ConfirmedAt(SalonTime.Now.AddHours(3));

        var moved = booking.Reschedule(SalonTime.Now.AddDays(3), booking.StaffId);

        Assert.Equal(BookingStatus.Rescheduled, booking.Status);
        Assert.Equal(BookingStatus.Requested, moved.Status);
    }

    [Fact]
    public void A_request_for_one_hour_from_now_cannot_be_confirmed_under_a_two_hour_notice()
    {
        // Default policy: two hours' notice.
        var booking = Booking.CreateBookingRequest(
            UserId.From(Guid.NewGuid()),
            ProviderId.New(),
            ServiceId.New(),
            Guid.NewGuid(),
            SalonTime.Now.AddHours(1),
            Duration.FromMinutes(30),
            Price.Create(100, PlatformCurrency.Code),
            BookingPolicy.Default);

        Assert.Throws<BusinessRuleViolationException>(() => booking.Confirm());
    }
}

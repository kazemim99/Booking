using System.Text.RegularExpressions;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.BookingAggregate;

/// <summary>
/// A customer or salon who breaks a booking rule is told so in Persian. The rules' messages travel straight to the
/// apps' snackbars, and they were English («Rescheduling must be done at least 24 hours before the booking») — QA
/// 2026-09-24. The code (`BOOKING_RESCHEDULE_WINDOW_EXPIRED`, …) stays English: it is for programs, the message for people.
/// </summary>
public class BookingRulesSpeakPersianTests
{
    private static Booking Confirmed(DateTime salonStart, BookingPolicy? policy = null) =>
        Booking.CreateConfirmedByProvider(
            UserId.From(Guid.NewGuid()), ProviderId.New(), ServiceId.New(), Guid.NewGuid(), salonStart,
            Duration.FromMinutes(60), Price.Create(100, PlatformCurrency.Code), policy ?? BookingPolicy.Default);

    private static void AssertPersian(Action rule)
    {
        var ex = Assert.Throws<BusinessRuleViolationException>(rule);
        Assert.Matches(new Regex("[؀-ۿ]"), ex.Message);
        Assert.DoesNotMatch(new Regex("[A-Za-z]{4,}"), ex.Message.Replace("BOOKING_", ""));
    }

    [Fact]
    public void Rescheduling_inside_the_window_says_how_many_hours_ahead_it_must_be_done()
    {
        // Default policy: two hours (openspec/changes/_inline/reviews-and-reschedule-round2 D5).
        var booking = Confirmed(SalonTime.Now.AddHours(1));

        var ex = Assert.Throws<BusinessRuleViolationException>(
            () => booking.Reschedule(SalonTime.Now.AddDays(3), booking.StaffId));

        Assert.Contains("تا 2 ساعت پیش از نوبت", ex.Message);
        Assert.Matches(new Regex("[؀-ۿ]"), ex.Message);
    }

    [Fact]
    public void Rescheduling_when_the_policy_does_not_allow_it_is_persian() =>
        AssertPersian(() => Confirmed(SalonTime.Now.AddDays(3), BookingPolicy.Create(2, 90, 24, 50, false, 24, false, 0))
            .Reschedule(SalonTime.Now.AddDays(4), Guid.NewGuid()));

    [Fact]
    public void Cancelling_a_booking_that_is_already_cancelled_is_persian()
    {
        var booking = Confirmed(SalonTime.Now.AddDays(3));
        booking.Cancel("changed my mind");

        AssertPersian(() => booking.Cancel("again"));
    }

    [Fact]
    public void Confirming_a_booking_that_is_not_a_request_is_persian() =>
        AssertPersian(() => Confirmed(SalonTime.Now.AddDays(3)).Confirm());

    [Fact]
    public void Completing_a_request_that_was_never_confirmed_is_persian()
    {
        var request = Booking.CreateBookingRequest(
            UserId.From(Guid.NewGuid()), ProviderId.New(), ServiceId.New(), Guid.NewGuid(),
            SalonTime.Now.AddDays(3), Duration.FromMinutes(30), Price.Create(100, PlatformCurrency.Code), BookingPolicy.Default);

        AssertPersian(() => request.Complete());
    }

    [Fact]
    public void Completing_long_before_the_appointment_is_persian() =>
        AssertPersian(() => Confirmed(SalonTime.Now.AddDays(3)).Complete());

    [Fact]
    public void Marking_a_no_show_before_the_appointment_ended_is_persian() =>
        AssertPersian(() => Confirmed(SalonTime.Now.AddDays(3)).MarkAsNoShow());

    [Fact]
    public void Marking_a_no_show_on_a_booking_that_is_not_confirmed_is_persian()
    {
        var request = Booking.CreateBookingRequest(
            UserId.From(Guid.NewGuid()), ProviderId.New(), ServiceId.New(), Guid.NewGuid(),
            SalonTime.Now.AddDays(3), Duration.FromMinutes(30), Price.Create(100, PlatformCurrency.Code), BookingPolicy.Default);

        AssertPersian(() => request.MarkAsNoShow());
    }

    [Fact]
    public void Confirming_too_close_to_the_appointment_is_persian()
    {
        // Two hours' notice is the default; one hour ahead is inside it.
        var late = Booking.CreateBookingRequest(
            UserId.From(Guid.NewGuid()), ProviderId.New(), ServiceId.New(), Guid.NewGuid(),
            SalonTime.Now.AddHours(1), Duration.FromMinutes(30), Price.Create(100, PlatformCurrency.Code), BookingPolicy.Default);

        AssertPersian(() => late.Confirm());
    }
}

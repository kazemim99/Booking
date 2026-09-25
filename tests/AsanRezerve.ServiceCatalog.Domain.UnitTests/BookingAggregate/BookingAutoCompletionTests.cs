using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Events;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.BookingAggregate;

/// <summary>
/// A confirmed visit the salon never marks done — nor no-show — becomes done by itself 12 hours after its end, so the
/// customer can review it (openspec/changes/_inline/reviews-and-reschedule-round2 D3). Before, only the salon could
/// complete a booking, and a salon that never pressed «انجام شد» could never be reviewed.
/// </summary>
public class BookingAutoCompletionTests
{
    /// <summary>
    /// The moment the pass runs, passed in. Real time rather than a fixed date only because the salon's own
    /// operations used to arrange a case (no-show) read the real clock.
    /// </summary>
    private static readonly DateTime UtcNow = DateTime.UtcNow;

    private static DateTime SalonNow => SalonTime.FromUtc(UtcNow);

    private static Booking ConfirmedEndingAt(DateTime salonEnd, int minutes = 60) =>
        Booking.CreateConfirmedByProvider(
            UserId.From(Guid.NewGuid()),
            ProviderId.New(),
            ServiceId.New(),
            Guid.NewGuid(),
            salonEnd.AddMinutes(-minutes),
            Duration.FromMinutes(minutes),
            Price.Create(100, PlatformCurrency.Code),
            BookingPolicy.Default);

    [Fact]
    public void The_wait_is_twelve_hours()
    {
        Assert.Equal(TimeSpan.FromHours(12), BookingAutoCompletion.After);
    }

    [Fact]
    public void A_booking_is_due_twelve_hours_after_its_end_on_the_salons_clock_and_not_a_minute_before()
    {
        Assert.True(BookingAutoCompletion.IsDue(SalonNow.AddHours(-12), UtcNow));
        Assert.True(BookingAutoCompletion.IsDue(SalonNow.AddHours(-30), UtcNow));
        Assert.False(BookingAutoCompletion.IsDue(SalonNow.AddHours(-12).AddMinutes(1), UtcNow));

        // Read against UTC, an end 10 hours ago on the salon's clock would look 13.5 hours old.
        Assert.False(BookingAutoCompletion.IsDue(SalonNow.AddHours(-10), UtcNow));
    }

    [Fact]
    public void The_latest_due_end_is_the_salons_now_less_twelve_hours()
    {
        Assert.Equal(SalonNow.AddHours(-12), BookingAutoCompletion.LatestDueEnd(UtcNow));
    }

    [Fact]
    public void A_confirmed_booking_twelve_hours_past_its_end_completes_by_itself_and_can_be_reviewed()
    {
        var booking = ConfirmedEndingAt(SalonNow.AddHours(-13));
        booking.ClearDomainEvents();

        booking.CompleteAutomatically(UtcNow);

        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(UtcNow, booking.CompletedAt);
        Assert.True(booking.CanBeReviewed());
        Assert.Contains(booking.History, h =>
            h.Status == BookingStatus.Completed && h.Description == Booking.AutoCompletedHistoryEntry);
    }

    [Fact]
    public void It_raises_the_same_completed_event_as_the_salon_marking_it_done()
    {
        var booking = ConfirmedEndingAt(SalonNow.AddHours(-13));
        booking.ClearDomainEvents();

        booking.CompleteAutomatically(UtcNow);

        var completed = Assert.Single(booking.DomainEvents.OfType<BookingCompletedEvent>());
        Assert.Equal(booking.Id, completed.BookingId);
    }

    [Fact]
    public void Before_the_twelve_hours_are_up_it_is_refused()
    {
        var booking = ConfirmedEndingAt(SalonNow.AddHours(-11));

        Assert.Throws<BusinessRuleViolationException>(() => booking.CompleteAutomatically(UtcNow));
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void Only_a_confirmed_booking_completes_by_itself()
    {
        var booking = ConfirmedEndingAt(SalonNow.AddHours(-13));
        booking.MarkAsNoShow("نیامد");

        Assert.Throws<BusinessRuleViolationException>(() => booking.CompleteAutomatically(UtcNow));
        Assert.Equal(BookingStatus.NoShow, booking.Status);
    }

    [Fact]
    public void The_customer_waiting_to_review_is_told_it_opens_by_itself_after_twelve_hours()
    {
        Assert.Contains("سالن", Booking.ReviewWaitsForTheSalonMessage);
        Assert.Contains("انجام‌شده", Booking.ReviewWaitsForTheSalonMessage);
        Assert.Contains("۱۲ ساعت", Booking.ReviewWaitsForTheSalonMessage);
    }
}

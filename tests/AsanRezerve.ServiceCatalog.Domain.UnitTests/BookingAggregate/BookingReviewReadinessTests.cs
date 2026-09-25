using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.BookingAggregate;

/// <summary>
/// When a booking may be reviewed, and what the customer is told when it may not yet
/// (openspec/changes/_inline/customer-reviews-and-nahal-seed). Only a visit on record as done can be reviewed, and only
/// the salon can put it on record — so a finished appointment the salon forgot showed no «ثبت نظر» and no reason.
/// </summary>
public class BookingReviewReadinessTests
{
    private static Booking ConfirmedAt(DateTime salonStart, int minutes = 60) =>
        Booking.CreateConfirmedByProvider(
            UserId.From(Guid.NewGuid()),
            ProviderId.New(),
            ServiceId.New(),
            Guid.NewGuid(),
            salonStart,
            Duration.FromMinutes(minutes),
            Price.Create(100, PlatformCurrency.Code),
            BookingPolicy.Default);

    [Fact]
    public void A_completed_visit_can_be_reviewed_and_nothing_stands_in_the_way()
    {
        var booking = ConfirmedAt(SalonTime.Now.AddHours(-2));
        booking.Complete();

        Assert.True(booking.CanBeReviewed());
        Assert.Null(booking.ReviewBlockedReason());
        Assert.Null(booking.ReviewRefusal());
    }

    [Fact]
    public void A_finished_visit_the_salon_has_not_marked_done_waits_for_the_salon()
    {
        var booking = ConfirmedAt(SalonTime.Now.AddHours(-2), minutes: 60);

        Assert.False(booking.CanBeReviewed());
        Assert.Equal(Booking.ReviewWaitsForTheSalonMessage, booking.ReviewBlockedReason());
        Assert.Equal(Booking.ReviewWaitsForTheSalonMessage, booking.ReviewRefusal());
    }

    [Fact]
    public void A_visit_still_under_way_is_not_explained_yet()
    {
        var booking = ConfirmedAt(SalonTime.Now.AddMinutes(-10), minutes: 60);

        Assert.Null(booking.ReviewBlockedReason());
    }

    [Fact]
    public void A_visit_still_ahead_is_not_explained()
    {
        var booking = ConfirmedAt(SalonTime.Now.AddDays(1));

        Assert.Null(booking.ReviewBlockedReason());
        Assert.Contains("تأیید شده", booking.ReviewRefusal());
    }

    [Fact]
    public void A_cancelled_booking_is_refused_naming_its_state_in_persian()
    {
        var booking = ConfirmedAt(SalonTime.Now.AddDays(3));
        booking.Cancel("منصرف شدم", byProvider: false);

        Assert.Null(booking.ReviewBlockedReason());
        Assert.Contains("لغو شده", booking.ReviewRefusal());
    }
}

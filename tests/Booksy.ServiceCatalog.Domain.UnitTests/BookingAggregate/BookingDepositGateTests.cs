using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Xunit;

namespace Booksy.ServiceCatalog.Domain.UnitTests.BookingAggregate;

/// <summary>
/// C4 deposit gating (create-then-pay). A deposit-required booking is created in the Requested state and cannot be
/// confirmed until the deposit is recorded as paid (via the verified deposit payment). This locks in the domain
/// half of the customer-payment-experience coupling.
/// </summary>
public class BookingDepositGateTests
{
    private static Booking NewBooking(bool requireDeposit, decimal depositPercentage = 20m)
    {
        var policy = BookingPolicy.Create(
            minAdvanceBookingHours: 1, maxAdvanceBookingDays: 90, cancellationWindowHours: 24,
            cancellationFeePercentage: 0m, allowRescheduling: true, rescheduleWindowHours: 24,
            requireDeposit: requireDeposit, depositPercentage: depositPercentage);

        return Booking.CreateBookingRequest(
            UserId.From(Guid.NewGuid()), ProviderId.From(Guid.NewGuid()), ServiceId.From(Guid.NewGuid()),
            staffId: Guid.NewGuid(), startTime: DateTime.UtcNow.AddDays(2), duration: Duration.FromMinutes(60),
            totalPrice: Price.Create(100m, "USD"), policy: policy);
    }

    [Fact]
    public void A_deposit_required_booking_cannot_be_confirmed_until_the_deposit_is_paid()
    {
        var booking = NewBooking(requireDeposit: true);
        Assert.Equal(BookingStatus.Requested, booking.Status);
        Assert.False(booking.PaymentInfo.IsDepositPaid());

        // Confirm is blocked before the deposit is paid.
        Assert.Throws<BusinessRuleViolationException>(() => booking.Confirm());

        // Record the verified deposit → now confirmable.
        booking.RecordDepositPaid("REF-DEPOSIT-1");
        Assert.True(booking.PaymentInfo.IsDepositPaid());

        booking.Confirm();
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void Recording_a_deposit_is_idempotent()
    {
        var booking = NewBooking(requireDeposit: true);
        booking.RecordDepositPaid("REF-1");
        var paidAfterFirst = booking.PaymentInfo.PaidAmount.Amount;

        booking.RecordDepositPaid("REF-1"); // replay
        booking.RecordDepositPaid("REF-2"); // duplicate

        Assert.Equal(paidAfterFirst, booking.PaymentInfo.PaidAmount.Amount); // never double-counted
    }

    [Fact]
    public void A_no_deposit_booking_confirms_without_any_payment()
    {
        var booking = NewBooking(requireDeposit: false);

        booking.Confirm(); // no deposit gate
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }
}

// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/BookingAggregate/States/RequestedState.cs
// ========================================
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate.States
{
    /// <summary>
    /// State: Booking has been requested but not yet confirmed
    /// Valid transitions: Confirm, Cancel
    /// </summary>
    public sealed class RequestedState : BookingStateBase
    {
        public override BookingStatus Status => BookingStatus.Requested;

        public override void Confirm(Booking booking)
        {
            // Business rules validation
            if (booking.Policy.RequireDeposit && !booking.PaymentInfo.IsDepositPaid())
                throw new BusinessRuleViolationException(
                    new DepositMustBePaidBeforeConfirmationRule());

            // Check if booking time is still valid
            if (!booking.Policy.IsWithinBookingWindow(booking.TimeSlot.StartTime, DateTime.UtcNow))
                throw new BusinessRuleViolationException(
                    new BookingMustBeWithinValidTimeWindowRule(booking.Policy));

            // Transition to Confirmed state
            booking.TransitionToState(new ConfirmedState());
            booking.SetConfirmedAt(DateTime.UtcNow);
            booking.AddHistoryEntry("Booking confirmed", BookingStatus.Confirmed);

            booking.RaiseDomainEvent(new BookingConfirmedEvent(
                booking.Id,
                booking.CustomerId,
                booking.ProviderId,
                booking.ServiceId,
                booking.StaffId,
                booking.TimeSlot.StartTime,
                booking.TimeSlot.EndTime,
                booking.ConfirmedAt!.Value));
        }

        public override void Cancel(Booking booking, string reason, bool byProvider = false)
        {
            var now = DateTime.UtcNow;
            var canCancelWithoutFee = booking.Policy.CanCancelWithoutFee(booking.TimeSlot.StartTime, now);

            Money? cancellationFee = null;
            if (!canCancelWithoutFee && !byProvider && booking.PaymentInfo.IsDepositPaid())
            {
                cancellationFee = booking.Policy.CalculateCancellationFee(
                    Money.Create(booking.TotalPrice.Amount, booking.TotalPrice.Currency));
            }

            // Transition to Cancelled state
            booking.TransitionToState(new CancelledState());
            booking.SetCancellationReason(reason);
            booking.SetCancelledAt(now);

            booking.AddHistoryEntry(
                $"Booking cancelled: {reason} {(canCancelWithoutFee ? "(no fee)" : "(fee applied)")}",
                BookingStatus.Cancelled);

            booking.RaiseDomainEvent(new BookingCancelledEvent(
                booking.Id,
                booking.CustomerId,
                booking.ProviderId,
                booking.ServiceId,
                booking.StaffId,
                reason,
                canCancelWithoutFee,
                cancellationFee?.Amount ?? 0,
                byProvider,
                now));
        }
    }

    // Business rules (kept near the state for clarity)
    internal sealed class DepositMustBePaidBeforeConfirmationRule : Booksy.Core.Domain.Abstractions.Rules.IBusinessRule
    {
        public string Message => "Deposit must be paid before booking can be confirmed";
        public string ErrorCode => "BOOKING_DEPOSIT_NOT_PAID";
        public bool IsBroken() => true;
    }

    internal sealed class BookingMustBeWithinValidTimeWindowRule : Booksy.Core.Domain.Abstractions.Rules.IBusinessRule
    {
        private readonly ValueObjects.BookingPolicy _policy;

        public BookingMustBeWithinValidTimeWindowRule(ValueObjects.BookingPolicy policy)
        {
            _policy = policy;
        }

        public string Message => $"Booking must be made at least {_policy.MinAdvanceBookingHours} hours in advance and no more than {_policy.MaxAdvanceBookingDays} days in advance";
        public string ErrorCode => "BOOKING_OUTSIDE_TIME_WINDOW";
        public bool IsBroken() => true;
    }
}

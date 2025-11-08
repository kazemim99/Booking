// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/BookingAggregate/States/ConfirmedState.cs
// ========================================
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate.States
{
    /// <summary>
    /// State: Booking has been confirmed and scheduled
    /// Valid transitions: Complete, Cancel, Reschedule, MarkAsNoShow
    /// </summary>
    public sealed class ConfirmedState : BookingStateBase
    {
        public override BookingStatus Status => BookingStatus.Confirmed;

        public override void Complete(Booking booking, string? staffNotes = null)
        {
            // Booking should be completed only after or near the scheduled time
            var now = DateTime.UtcNow;
            if (now < booking.TimeSlot.StartTime.AddMinutes(-15)) // Allow 15 min early completion
                throw new BusinessRuleViolationException(
                    new BookingCannotBeCompletedBeforeScheduledTimeRule(booking.TimeSlot.StartTime));

            // Transition to Completed state
            booking.TransitionToState(new CompletedState());
            booking.SetCompletedAt(now);
            booking.SetStaffNotes(staffNotes);

            booking.AddHistoryEntry("Booking completed", BookingStatus.Completed);

            booking.RaiseDomainEvent(new BookingCompletedEvent(
                booking.Id,
                booking.CustomerId,
                booking.ProviderId,
                booking.ServiceId,
                booking.StaffId,
                booking.TimeSlot.StartTime,
                now));
        }

        public override void Cancel(Booking booking, string reason, bool byProvider = false)
        {
            var now = DateTime.UtcNow;

            // Check cancellation policy (24-hour window)
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

        public override Booking Reschedule(Booking booking, DateTime newStartTime, Guid newStaffId, string? reason = null)
        {
            // Check if rescheduling is allowed by policy
            if (!booking.Policy.AllowRescheduling)
                throw new BusinessRuleViolationException(
                    new BookingCannotBeRescheduledRule(Status, booking.Policy));

            var now = DateTime.UtcNow;

            // Check if within reschedule window (24-hour policy)
            if (!booking.Policy.CanReschedule(booking.TimeSlot.StartTime, now))
                throw new BusinessRuleViolationException(
                    new RescheduleWindowExpiredRule(booking.Policy, booking.TimeSlot.StartTime));

            // Create new booking for the rescheduled time
            var newBooking = booking.CreateRescheduledBooking(newStartTime, newStaffId, reason);

            // Transition current booking to Rescheduled state
            booking.TransitionToState(new RescheduledState());
            booking.SetRescheduledAt(now);
            booking.SetRescheduledToBookingId(newBooking.Id);

            booking.AddHistoryEntry(
                $"Booking rescheduled to {newStartTime:yyyy-MM-dd HH:mm}. {reason}",
                BookingStatus.Rescheduled);

            newBooking.AddHistoryEntry(
                $"Rescheduled from {booking.TimeSlot.StartTime:yyyy-MM-dd HH:mm}",
                BookingStatus.Requested);

            booking.RaiseDomainEvent(new BookingRescheduledEvent(
                booking.Id,
                newBooking.Id,
                booking.CustomerId,
                booking.ProviderId,
                booking.TimeSlot.StartTime,
                newStartTime,
                booking.StaffId,
                newStaffId,
                reason,
                now));

            return newBooking;
        }

        public override void MarkAsNoShow(Booking booking, string? reason = null)
        {
            // Can only mark as no-show after the scheduled time
            var now = DateTime.UtcNow;
            if (now < booking.TimeSlot.EndTime)
                throw new BusinessRuleViolationException(
                    new CannotMarkNoShowBeforeEndTimeRule(booking.TimeSlot.EndTime));

            // Transition to NoShow state
            booking.TransitionToState(new NoShowState());
            booking.SetStaffNotes(reason ?? "Customer did not show up");

            booking.AddHistoryEntry("Marked as no-show", BookingStatus.NoShow);

            booking.RaiseDomainEvent(new BookingNoShowEvent(
                booking.Id,
                booking.CustomerId,
                booking.ProviderId,
                booking.ServiceId,
                booking.StaffId,
                booking.TimeSlot.StartTime,
                booking.PaymentInfo.PaidAmount,
                now));
        }
    }

    // Business rules
    internal sealed class BookingCannotBeCompletedBeforeScheduledTimeRule : Booksy.Core.Domain.Abstractions.Rules.IBusinessRule
    {
        private readonly DateTime _scheduledTime;

        public BookingCannotBeCompletedBeforeScheduledTimeRule(DateTime scheduledTime)
        {
            _scheduledTime = scheduledTime;
        }

        public string Message => $"Booking cannot be completed before scheduled time: {_scheduledTime:yyyy-MM-dd HH:mm}";
        public string ErrorCode => "BOOKING_TOO_EARLY_TO_COMPLETE";
        public bool IsBroken() => true;
    }

    internal sealed class BookingCannotBeRescheduledRule : Booksy.Core.Domain.Abstractions.Rules.IBusinessRule
    {
        private readonly BookingStatus _status;
        private readonly BookingPolicy _policy;

        public BookingCannotBeRescheduledRule(BookingStatus status, BookingPolicy policy)
        {
            _status = status;
            _policy = policy;
        }

        public string Message => _policy.AllowRescheduling
            ? $"Booking with status {_status} cannot be rescheduled"
            : "Rescheduling is not allowed by booking policy";
        public string ErrorCode => "BOOKING_CANNOT_BE_RESCHEDULED";
        public bool IsBroken() => true;
    }

    internal sealed class RescheduleWindowExpiredRule : Booksy.Core.Domain.Abstractions.Rules.IBusinessRule
    {
        private readonly BookingPolicy _policy;
        private readonly DateTime _bookingStartTime;

        public RescheduleWindowExpiredRule(BookingPolicy policy, DateTime bookingStartTime)
        {
            _policy = policy;
            _bookingStartTime = bookingStartTime;
        }

        public string Message => $"Rescheduling must be done at least {_policy.RescheduleWindowHours} hours before the booking";
        public string ErrorCode => "BOOKING_RESCHEDULE_WINDOW_EXPIRED";
        public bool IsBroken() => true;
    }

    internal sealed class CannotMarkNoShowBeforeEndTimeRule : Booksy.Core.Domain.Abstractions.Rules.IBusinessRule
    {
        private readonly DateTime _endTime;

        public CannotMarkNoShowBeforeEndTimeRule(DateTime endTime)
        {
            _endTime = endTime;
        }

        public string Message => $"Cannot mark as no-show before booking end time: {_endTime:yyyy-MM-dd HH:mm}";
        public string ErrorCode => "BOOKING_TOO_EARLY_FOR_NO_SHOW";
        public bool IsBroken() => true;
    }
}

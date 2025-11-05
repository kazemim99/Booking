// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/BookingAggregate/Booking.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Abstractions.Rules;
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate
{
    /// <summary>
    /// Booking aggregate root - manages appointment bookings with customers
    /// </summary>
    public sealed class Booking : AggregateRoot<BookingId>, IAuditableEntity
    {
        private readonly List<BookingHistoryEntry> _history = new();

        // Core Identity
        public UserId CustomerId { get; private set; }
        public ProviderId ProviderId { get; private set; }
        public ServiceId ServiceId { get; private set; }
        public Guid StaffId { get; private set; }

        // Booking Details
        public TimeSlot TimeSlot { get; private set; }
        public Duration Duration { get; private set; }
        public BookingStatus Status { get; private set; }

        // Pricing & Payment
        public Price TotalPrice { get; private set; }
        public PaymentInfo PaymentInfo { get; private set; }

        // Policy & Rules
        public BookingPolicy Policy { get; private set; }

        // Additional Information
        public string? CustomerNotes { get; private set; }
        public string? StaffNotes { get; private set; }
        public string? CancellationReason { get; private set; }

        // Timestamps
        public DateTime RequestedAt { get; private set; }
        public DateTime? ConfirmedAt { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public DateTime? RescheduledAt { get; private set; }

        // Rescheduling
        public BookingId? PreviousBookingId { get; private set; }
        public BookingId? RescheduledToBookingId { get; private set; }

        // Collections
        public IReadOnlyList<BookingHistoryEntry> History => _history.AsReadOnly();

        // Audit Properties
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }

        // Private constructor for EF Core
        private Booking() : base() { }

        /// <summary>
        /// Factory method - Create a new booking request
        /// </summary>
        public static Booking CreateBookingRequest(
            UserId customerId,
            ProviderId providerId,
            ServiceId serviceId,
            Guid staffId,
            DateTime startTime,
            Duration duration,
            Price totalPrice,
            BookingPolicy policy,
            string? customerNotes = null)
        {
            var timeSlot = TimeSlot.Create(startTime, duration);
            var depositAmount = policy.CalculateDepositAmount(
                Money.Create(totalPrice.Amount, totalPrice.Currency));

            var paymentInfo = PaymentInfo.Create(
                Money.Create(totalPrice.Amount, totalPrice.Currency),
                depositAmount);

            var booking = new Booking
            {
                Id = BookingId.New(),
                CustomerId = customerId,
                ProviderId = providerId,
                ServiceId = serviceId,
                StaffId = staffId,
                TimeSlot = timeSlot,
                Duration = duration,
                Status = BookingStatus.Requested,
                TotalPrice = totalPrice,
                PaymentInfo = paymentInfo,
                Policy = policy,
                CustomerNotes = customerNotes,
                RequestedAt = DateTime.UtcNow
            };

            booking.AddHistoryEntry("Booking requested", BookingStatus.Requested);

            booking.RaiseDomainEvent(new BookingRequestedEvent(
                booking.Id,
                booking.CustomerId,
                booking.ProviderId,
                booking.ServiceId,
                booking.StaffId,
                booking.TimeSlot.StartTime,
                booking.TimeSlot.EndTime,
                booking.TotalPrice,
                booking.RequestedAt));

            return booking;
        }

        // ========================================
        // BUSINESS METHODS - STATE TRANSITIONS
        // ========================================

        /// <summary>
        /// Confirm the booking after validation and optional payment
        /// </summary>
        public void Confirm()
        {
            // Business rules validation
            if (Status != BookingStatus.Requested)
                throw new BusinessRuleViolationException(
                    new BookingCanOnlyBeConfirmedFromRequestedStateRule(Status));

            // Check if deposit is required and paid
            if (Policy.RequireDeposit && !PaymentInfo.IsDepositPaid())
                throw new BusinessRuleViolationException(
                    new DepositMustBePaidBeforeConfirmationRule());

            // Check if booking time is still valid
            if (!Policy.IsWithinBookingWindow(TimeSlot.StartTime, DateTime.UtcNow))
                throw new BusinessRuleViolationException(
                    new BookingMustBeWithinValidTimeWindowRule(Policy));

            Status = BookingStatus.Confirmed;
            ConfirmedAt = DateTime.UtcNow;

            AddHistoryEntry("Booking confirmed", BookingStatus.Confirmed);

            RaiseDomainEvent(new BookingConfirmedEvent(
                Id,
                CustomerId,
                ProviderId,
                ServiceId,
                StaffId,
                TimeSlot.StartTime,
                TimeSlot.EndTime,
                ConfirmedAt.Value));
        }

        /// <summary>
        /// Cancel the booking with optional reason
        /// </summary>
        public void Cancel(string reason, bool byProvider = false)
        {
            // Business rules validation
            if (!CanBeCancelled())
                throw new BusinessRuleViolationException(
                    new BookingCannotBeCancelledRule(Status));

            var now = DateTime.UtcNow;
            var canCancelWithoutFee = Policy.CanCancelWithoutFee(TimeSlot.StartTime, now);

            Money? cancellationFee = null;
            if (!canCancelWithoutFee && !byProvider && PaymentInfo.IsDepositPaid())
            {
                cancellationFee = Policy.CalculateCancellationFee(
                    Money.Create(TotalPrice.Amount, TotalPrice.Currency));
            }

            Status = BookingStatus.Cancelled;
            CancellationReason = reason;
            CancelledAt = now;

            AddHistoryEntry(
                $"Booking cancelled: {reason} {(canCancelWithoutFee ? "(no fee)" : "(fee applied)")}",
                BookingStatus.Cancelled);

            RaiseDomainEvent(new BookingCancelledEvent(
                Id,
                CustomerId,
                ProviderId,
                ServiceId,
                StaffId,
                reason,
                canCancelWithoutFee,
                cancellationFee?.Amount ?? 0,
                byProvider,
                CancelledAt.Value));
        }

        /// <summary>
        /// Reschedule the booking to a new time
        /// </summary>
        public Booking Reschedule(
            DateTime newStartTime,
            Guid newStaffId,
            string? reason = null)
        {
            // Business rules validation
            if (!CanBeRescheduled())
                throw new BusinessRuleViolationException(
                    new BookingCannotBeRescheduledRule(Status, Policy));

            var now = DateTime.UtcNow;
            if (!Policy.CanReschedule(TimeSlot.StartTime, now))
                throw new BusinessRuleViolationException(
                    new RescheduleWindowExpiredRule(Policy, TimeSlot.StartTime));

            // Create new booking for the rescheduled time
            var newBooking = new Booking
            {
                Id = BookingId.New(),
                CustomerId = CustomerId,
                ProviderId = ProviderId,
                ServiceId = ServiceId,
                StaffId = newStaffId,
                TimeSlot = TimeSlot.Create(newStartTime, Duration),
                Duration = Duration,
                Status = BookingStatus.Requested,
                TotalPrice = TotalPrice,
                PaymentInfo = PaymentInfo, // Transfer payment info
                Policy = Policy,
                CustomerNotes = CustomerNotes,
                PreviousBookingId = Id,
                RequestedAt = now
            };

            // Mark current booking as rescheduled
            Status = BookingStatus.Rescheduled;
            RescheduledToBookingId = newBooking.Id;
            RescheduledAt = now;

            AddHistoryEntry(
                $"Booking rescheduled to {newStartTime:yyyy-MM-dd HH:mm}. {reason}",
                BookingStatus.Rescheduled);

            newBooking.AddHistoryEntry(
                $"Rescheduled from {TimeSlot.StartTime:yyyy-MM-dd HH:mm}",
                BookingStatus.Requested);

            RaiseDomainEvent(new BookingRescheduledEvent(
                Id,
                newBooking.Id,
                CustomerId,
                ProviderId,
                TimeSlot.StartTime,
                newStartTime,
                StaffId,
                newStaffId,
                reason,
                RescheduledAt.Value));

            return newBooking;
        }

        /// <summary>
        /// Mark booking as completed after service is provided
        /// </summary>
        public void Complete(string? staffNotes = null)
        {
            // Business rules validation
            if (Status != BookingStatus.Confirmed)
                throw new BusinessRuleViolationException(
                    new BookingCanOnlyBeCompletedFromConfirmedStateRule(Status));

            // Booking should be completed only after or near the scheduled time
            var now = DateTime.UtcNow;
            if (now < TimeSlot.StartTime.AddMinutes(-15)) // Allow 15 min early completion
                throw new BusinessRuleViolationException(
                    new BookingCannotBeCompletedBeforeScheduledTimeRule(TimeSlot.StartTime));

            Status = BookingStatus.Completed;
            CompletedAt = now;
            StaffNotes = staffNotes;

            AddHistoryEntry("Booking completed", BookingStatus.Completed);

            RaiseDomainEvent(new BookingCompletedEvent(
                Id,
                CustomerId,
                ProviderId,
                ServiceId,
                StaffId,
                TimeSlot.StartTime,
                CompletedAt.Value));
        }

        /// <summary>
        /// Mark booking as no-show when customer doesn't appear
        /// </summary>
        public void MarkAsNoShow(string? reason = null)
        {
            // Business rules validation
            if (Status != BookingStatus.Confirmed)
                throw new BusinessRuleViolationException(
                    new NoShowCanOnlyBeMarkedFromConfirmedStateRule(Status));

            // Can only mark as no-show after the scheduled time
            var now = DateTime.UtcNow;
            if (now < TimeSlot.EndTime)
                throw new BusinessRuleViolationException(
                    new CannotMarkNoShowBeforeEndTimeRule(TimeSlot.EndTime));

            Status = BookingStatus.NoShow;
            StaffNotes = reason ?? "Customer did not show up";

            AddHistoryEntry("Marked as no-show", BookingStatus.NoShow);

            RaiseDomainEvent(new BookingNoShowEvent(
                Id,
                CustomerId,
                ProviderId,
                ServiceId,
                StaffId,
                TimeSlot.StartTime,
                PaymentInfo.PaidAmount,
                now));
        }

        // ========================================
        // PAYMENT METHODS
        // ========================================

        /// <summary>
        /// Process deposit payment
        /// </summary>
        public void ProcessDepositPayment(string paymentIntentId)
        {
            if (Status != BookingStatus.Requested)
                throw new InvalidOperationException("Deposit can only be paid for requested bookings");

            PaymentInfo = PaymentInfo.WithDepositPaid(paymentIntentId);

            AddHistoryEntry($"Deposit paid: {PaymentInfo.DepositAmount}", Status);

            RaiseDomainEvent(new BookingPaymentProcessedEvent(
                Id,
                CustomerId,
                PaymentInfo.DepositAmount,
                PaymentInfo.Status,
                paymentIntentId,
                DateTime.UtcNow));
        }

        /// <summary>
        /// Process full payment
        /// </summary>
        public void ProcessFullPayment(string paymentIntentId)
        {
            if (Status == BookingStatus.Cancelled || Status == BookingStatus.NoShow)
                throw new InvalidOperationException($"Cannot process payment for {Status} booking");

            PaymentInfo = PaymentInfo.WithFullPayment(paymentIntentId);

            AddHistoryEntry($"Full payment received: {PaymentInfo.TotalAmount}", Status);

            RaiseDomainEvent(new BookingPaymentProcessedEvent(
                Id,
                CustomerId,
                PaymentInfo.TotalAmount,
                PaymentInfo.Status,
                paymentIntentId,
                DateTime.UtcNow));
        }

        /// <summary>
        /// Process refund
        /// </summary>
        public void ProcessRefund(Money refundAmount, string refundId, string reason)
        {
            if (Status != BookingStatus.Cancelled)
                throw new InvalidOperationException("Refunds can only be processed for cancelled bookings");

            PaymentInfo = PaymentInfo.WithRefund(refundAmount, refundId);

            AddHistoryEntry($"Refund processed: {refundAmount}. Reason: {reason}", Status);

            RaiseDomainEvent(new BookingRefundProcessedEvent(
                Id,
                CustomerId,
                refundAmount,
                PaymentInfo.Status,
                refundId,
                reason,
                DateTime.UtcNow));
        }

        // ========================================
        // QUERY METHODS
        // ========================================

        /// <summary>
        /// Check if booking can be cancelled
        /// </summary>
        public bool CanBeCancelled()
        {
            return Status == BookingStatus.Requested ||
                   Status == BookingStatus.Confirmed;
        }

        /// <summary>
        /// Check if booking can be rescheduled
        /// </summary>
        public bool CanBeRescheduled()
        {
            if (!Policy.AllowRescheduling)
                return false;

            return Status == BookingStatus.Requested ||
                   Status == BookingStatus.Confirmed;
        }

        /// <summary>
        /// Check if booking is in the past
        /// </summary>
        public bool IsInPast()
        {
            return TimeSlot.EndTime < DateTime.UtcNow;
        }

        /// <summary>
        /// Check if booking is upcoming (within next 24 hours)
        /// </summary>
        public bool IsUpcoming()
        {
            var now = DateTime.UtcNow;
            return TimeSlot.StartTime > now && TimeSlot.StartTime <= now.AddHours(24);
        }

        /// <summary>
        /// Get remaining payment amount
        /// </summary>
        public Money GetRemainingPayment()
        {
            return PaymentInfo.GetRemainingAmount();
        }

        // ========================================
        // HELPER METHODS
        // ========================================

        /// <summary>
        /// Add customer notes
        /// </summary>
        public void UpdateCustomerNotes(string notes)
        {
            CustomerNotes = notes;
            AddHistoryEntry("Customer notes updated", Status);
        }

        /// <summary>
        /// Add staff notes (internal)
        /// </summary>
        public void UpdateStaffNotes(string notes)
        {
            StaffNotes = notes;
            AddHistoryEntry("Staff notes updated", Status);
        }

        /// <summary>
        /// Add entry to booking history
        /// </summary>
        private void AddHistoryEntry(string description, BookingStatus status)
        {
            var entry = BookingHistoryEntry.Create(description, status);
            _history.Add(entry);
        }
    }

    // ========================================
    // BUSINESS RULES
    // ========================================

    internal sealed class BookingCanOnlyBeConfirmedFromRequestedStateRule : IBusinessRule
    {
        private readonly BookingStatus _currentStatus;

        public BookingCanOnlyBeConfirmedFromRequestedStateRule(BookingStatus currentStatus)
        {
            _currentStatus = currentStatus;
        }

        public string Message => $"Booking can only be confirmed from Requested state. Current state: {_currentStatus}";
        public string ErrorCode => "BOOKING_INVALID_STATE_FOR_CONFIRMATION";
        public bool IsBroken() => _currentStatus != BookingStatus.Requested;
    }

    internal sealed class DepositMustBePaidBeforeConfirmationRule : IBusinessRule
    {
        public string Message => "Deposit must be paid before booking can be confirmed";
        public string ErrorCode => "BOOKING_DEPOSIT_NOT_PAID";
        public bool IsBroken() => true;
    }

    internal sealed class BookingMustBeWithinValidTimeWindowRule : IBusinessRule
    {
        private readonly BookingPolicy _policy;

        public BookingMustBeWithinValidTimeWindowRule(BookingPolicy policy)
        {
            _policy = policy;
        }

        public string Message => $"Booking must be made at least {_policy.MinAdvanceBookingHours} hours in advance and no more than {_policy.MaxAdvanceBookingDays} days in advance";
        public string ErrorCode => "BOOKING_OUTSIDE_TIME_WINDOW";
        public bool IsBroken() => true;
    }

    internal sealed class BookingCannotBeCancelledRule : IBusinessRule
    {
        private readonly BookingStatus _status;

        public BookingCannotBeCancelledRule(BookingStatus status)
        {
            _status = status;
        }

        public string Message => $"Booking with status {_status} cannot be cancelled";
        public string ErrorCode => "BOOKING_CANNOT_BE_CANCELLED";
        public bool IsBroken() => true;
    }

    internal sealed class BookingCannotBeRescheduledRule : IBusinessRule
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

    internal sealed class RescheduleWindowExpiredRule : IBusinessRule
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

    internal sealed class BookingCanOnlyBeCompletedFromConfirmedStateRule : IBusinessRule
    {
        private readonly BookingStatus _status;

        public BookingCanOnlyBeCompletedFromConfirmedStateRule(BookingStatus status)
        {
            _status = status;
        }

        public string Message => $"Booking can only be completed from Confirmed state. Current state: {_status}";
        public string ErrorCode => "BOOKING_INVALID_STATE_FOR_COMPLETION";
        public bool IsBroken() => true;
    }

    internal sealed class BookingCannotBeCompletedBeforeScheduledTimeRule : IBusinessRule
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

    internal sealed class NoShowCanOnlyBeMarkedFromConfirmedStateRule : IBusinessRule
    {
        private readonly BookingStatus _status;

        public NoShowCanOnlyBeMarkedFromConfirmedStateRule(BookingStatus status)
        {
            _status = status;
        }

        public string Message => $"No-show can only be marked for Confirmed bookings. Current state: {_status}";
        public string ErrorCode => "BOOKING_INVALID_STATE_FOR_NO_SHOW";
        public bool IsBroken() => true;
    }

    internal sealed class CannotMarkNoShowBeforeEndTimeRule : IBusinessRule
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

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
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate.States;

namespace Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate
{
    /// <summary>
    /// Booking aggregate root - manages appointment bookings with customers
    /// Uses State pattern for managing booking status transitions
    /// </summary>
    public sealed class Booking : AggregateRoot<BookingId>, IAuditableEntity
    {
        private readonly List<BookingHistoryEntry> _history = new();
        private IBookingState _state;

        // Core Identity
        public UserId CustomerId { get; private set; }
        public ProviderId ProviderId { get; private set; }
        public ServiceId ServiceId { get; private set; }
        public Guid StaffId { get; private set; }

        // Booking Details
        public TimeSlot TimeSlot { get; private set; }
        public Duration Duration { get; private set; }
        public BookingStatus Status => _state.Status;

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
        private Booking() : base()
        {
            _state = new RequestedState(); // Default state
        }

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
                TotalPrice = totalPrice,
                PaymentInfo = paymentInfo,
                Policy = policy,
                CustomerNotes = customerNotes,
                RequestedAt = DateTime.UtcNow,
                _state = new RequestedState()
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
        // BUSINESS METHODS - STATE TRANSITIONS (Using State Pattern)
        // ========================================

        /// <summary>
        /// Confirm the booking after validation and optional payment
        /// </summary>
        public void Confirm()
        {
            _state.Confirm(this);
        }

        /// <summary>
        /// Cancel the booking with optional reason
        /// </summary>
        public void Cancel(string reason, bool byProvider = false)
        {
            _state.Cancel(this, reason, byProvider);
        }

        /// <summary>
        /// Reschedule the booking to a new time
        /// </summary>
        public Booking Reschedule(
            DateTime newStartTime,
            Guid newStaffId,
            string? reason = null)
        {
            return _state.Reschedule(this, newStartTime, newStaffId, reason);
        }

        /// <summary>
        /// Mark booking as completed after service is provided
        /// </summary>
        public void Complete(string? staffNotes = null)
        {
            _state.Complete(this, staffNotes);
        }

        /// <summary>
        /// Mark booking as no-show when customer doesn't appear
        /// </summary>
        public void MarkAsNoShow(string? reason = null)
        {
            _state.MarkAsNoShow(this, reason);
        }

        /// <summary>
        /// Assign or reassign staff to the booking
        /// </summary>
        public void AssignStaff(Guid newStaffId)
        {
            // Can only assign staff to Requested or Confirmed bookings
            if (Status != BookingStatus.Requested && Status != BookingStatus.Confirmed)
                throw new BusinessRuleViolationException(
                    new StaffCanOnlyBeAssignedToActiveBookingsRule(Status));

            // No change needed
            if (StaffId == newStaffId)
                return;

            var previousStaffId = StaffId;
            StaffId = newStaffId;

            AddHistoryEntry($"Staff reassigned from {previousStaffId} to {newStaffId}", Status);

            RaiseDomainEvent(new StaffAssignedToBookingEvent(
                Id,
                CustomerId,
                ProviderId,
                ServiceId,
                previousStaffId,
                newStaffId,
                TimeSlot.StartTime,
                DateTime.UtcNow));
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
        public void UpdateCustomerNotes(string notes, string addedBy)
        {
            CustomerNotes = notes;
            AddHistoryEntry("Customer notes updated", Status);

            RaiseDomainEvent(new BookingNotesAddedEvent(
                Id,
                CustomerId,
                ProviderId,
                notes,
                addedBy,
                IsStaffNote: false,
                DateTime.UtcNow));
        }

        /// <summary>
        /// Add staff notes (internal)
        /// </summary>
        public void UpdateStaffNotes(string notes, string addedBy)
        {
            StaffNotes = notes;
            AddHistoryEntry("Staff notes updated", Status);

            RaiseDomainEvent(new BookingNotesAddedEvent(
                Id,
                CustomerId,
                ProviderId,
                notes,
                addedBy,
                IsStaffNote: true,
                DateTime.UtcNow));
        }

        /// <summary>
        /// Add entry to booking history
        /// </summary>
        internal void AddHistoryEntry(string description, BookingStatus status)
        {
            var entry = BookingHistoryEntry.Create(description, status);
            _history.Add(entry);
        }

        // ========================================
        // INTERNAL STATE TRANSITION HELPERS
        // These methods are called by state classes to modify the booking
        // ========================================

        /// <summary>
        /// Transition to a new state
        /// </summary>
        internal void TransitionToState(IBookingState newState)
        {
            _state = newState;
        }

        /// <summary>
        /// Set the confirmed timestamp
        /// </summary>
        internal void SetConfirmedAt(DateTime confirmedAt)
        {
            ConfirmedAt = confirmedAt;
        }

        /// <summary>
        /// Set the cancelled timestamp
        /// </summary>
        internal void SetCancelledAt(DateTime cancelledAt)
        {
            CancelledAt = cancelledAt;
        }

        /// <summary>
        /// Set the cancellation reason
        /// </summary>
        internal void SetCancellationReason(string reason)
        {
            CancellationReason = reason;
        }

        /// <summary>
        /// Set the completed timestamp
        /// </summary>
        internal void SetCompletedAt(DateTime completedAt)
        {
            CompletedAt = completedAt;
        }

        /// <summary>
        /// Set staff notes
        /// </summary>
        internal void SetStaffNotes(string? notes)
        {
            StaffNotes = notes;
        }

        /// <summary>
        /// Set the rescheduled timestamp
        /// </summary>
        internal void SetRescheduledAt(DateTime rescheduledAt)
        {
            RescheduledAt = rescheduledAt;
        }

        /// <summary>
        /// Set the rescheduled-to booking ID
        /// </summary>
        internal void SetRescheduledToBookingId(BookingId bookingId)
        {
            RescheduledToBookingId = bookingId;
        }

        /// <summary>
        /// Create a new booking for rescheduling
        /// </summary>
        internal Booking CreateRescheduledBooking(DateTime newStartTime, Guid newStaffId, string? reason)
        {
            var now = DateTime.UtcNow;

            var newBooking = new Booking
            {
                Id = BookingId.New(),
                CustomerId = CustomerId,
                ProviderId = ProviderId,
                ServiceId = ServiceId,
                StaffId = newStaffId,
                TimeSlot = TimeSlot.Create(newStartTime, Duration),
                Duration = Duration,
                TotalPrice = TotalPrice,
                PaymentInfo = PaymentInfo, // Transfer payment info
                Policy = Policy,
                CustomerNotes = CustomerNotes,
                PreviousBookingId = Id,
                RequestedAt = now,
                _state = new RequestedState()
            };

            return newBooking;
        }
    }

    // ========================================
    // BUSINESS RULES (kept for non-state-transition operations)
    // ========================================

    internal sealed class StaffCanOnlyBeAssignedToActiveBookingsRule : IBusinessRule
    {
        private readonly BookingStatus _status;

        public StaffCanOnlyBeAssignedToActiveBookingsRule(BookingStatus status)
        {
            _status = status;
        }

        public string Message => $"Staff can only be assigned to Requested or Confirmed bookings. Current state: {_status}";
        public string ErrorCode => "BOOKING_INVALID_STATE_FOR_STAFF_ASSIGNMENT";
        public bool IsBroken() => true;
    }
}

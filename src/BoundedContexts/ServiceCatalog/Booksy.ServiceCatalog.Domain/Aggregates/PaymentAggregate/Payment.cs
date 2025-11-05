// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/PaymentAggregate/Payment.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate
{
    /// <summary>
    /// Payment aggregate root - manages payment lifecycle and transactions
    /// </summary>
    public sealed class Payment : AggregateRoot<PaymentId>
    {
        private readonly List<Transaction> _transactions = new();

        // Identity
        public BookingId? BookingId { get; private set; }
        public UserId CustomerId { get; private set; }
        public ProviderId ProviderId { get; private set; }

        // Payment Details
        public Money Amount { get; private set; }
        public Money PaidAmount { get; private set; }
        public Money RefundedAmount { get; private set; }
        public PaymentStatus Status { get; private set; }
        public PaymentMethod Method { get; private set; }

        // Payment Provider Details
        public string? PaymentIntentId { get; private set; }
        public string? PaymentMethodId { get; private set; }
        public string? CustomCustomerId { get; private set; } // Stripe/payment gateway customer ID

        // Timestamps
        public DateTime CreatedAt { get; private set; }
        public DateTime? AuthorizedAt { get; private set; }
        public DateTime? CapturedAt { get; private set; }
        public DateTime? RefundedAt { get; private set; }
        public DateTime? FailedAt { get; private set; }

        // Additional Info
        public string? Description { get; private set; }
        public string? FailureReason { get; private set; }
        public Dictionary<string, string> Metadata { get; private set; }

        // Collections
        public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();

        // EF Core requires parameterless constructor
        private Payment()
        {
            Metadata = new Dictionary<string, string>();
            PaidAmount = Money.Zero("USD");
            RefundedAmount = Money.Zero("USD");
        }

        private Payment(
            PaymentId id,
            BookingId? bookingId,
            UserId customerId,
            ProviderId providerId,
            Money amount,
            PaymentMethod method,
            string? description,
            Dictionary<string, string>? metadata) : base(id)
        {
            BookingId = bookingId;
            CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
            ProviderId = providerId ?? throw new ArgumentNullException(nameof(providerId));
            Amount = amount ?? throw new ArgumentNullException(nameof(amount));
            PaidAmount = Money.Zero(amount.Currency);
            RefundedAmount = Money.Zero(amount.Currency);
            Status = PaymentStatus.Pending;
            Method = method;
            Description = description;
            CreatedAt = DateTime.UtcNow;
            Metadata = metadata ?? new Dictionary<string, string>();

            RaiseDomainEvent(new PaymentCreatedEvent(id, bookingId, customerId, providerId, amount, CreatedAt));
        }

        /// <summary>
        /// Creates a new payment for a booking
        /// </summary>
        public static Payment CreateForBooking(
            BookingId bookingId,
            UserId customerId,
            ProviderId providerId,
            Money amount,
            PaymentMethod method,
            string? description = null,
            Dictionary<string, string>? metadata = null)
        {
            if (amount.Amount <= 0)
                throw new InvalidOperationException("Payment amount must be positive");

            return new Payment(
                PaymentId.New(),
                bookingId,
                customerId,
                providerId,
                amount,
                method,
                description,
                metadata);
        }

        /// <summary>
        /// Creates a direct payment (not associated with a booking)
        /// </summary>
        public static Payment CreateDirect(
            UserId customerId,
            ProviderId providerId,
            Money amount,
            PaymentMethod method,
            string? description = null,
            Dictionary<string, string>? metadata = null)
        {
            if (amount.Amount <= 0)
                throw new InvalidOperationException("Payment amount must be positive");

            return new Payment(
                PaymentId.New(),
                null,
                customerId,
                providerId,
                amount,
                method,
                description,
                metadata);
        }

        /// <summary>
        /// Authorizes payment (holds funds without capturing)
        /// </summary>
        public void Authorize(string paymentIntentId, string? paymentMethodId = null)
        {
            if (Status != PaymentStatus.Pending)
                throw new InvalidOperationException($"Cannot authorize payment in {Status} status");

            if (string.IsNullOrWhiteSpace(paymentIntentId))
                throw new ArgumentException("Payment intent ID is required", nameof(paymentIntentId));

            PaymentIntentId = paymentIntentId;
            PaymentMethodId = paymentMethodId;
            Status = PaymentStatus.Pending; // Still pending until captured
            AuthorizedAt = DateTime.UtcNow;

            var transaction = Transaction.CreateAuthorization(
                Amount,
                paymentIntentId,
                $"Authorization for Payment {Id}",
                Metadata);

            _transactions.Add(transaction);

            RaiseDomainEvent(new PaymentAuthorizedEvent(Id, BookingId, CustomerId, Amount, AuthorizedAt.Value));
        }

        /// <summary>
        /// Captures an authorized payment
        /// </summary>
        public void Capture(string? transactionId = null)
        {
            if (Status != PaymentStatus.Pending)
                throw new InvalidOperationException($"Cannot capture payment in {Status} status");

            if (AuthorizedAt == null)
                throw new InvalidOperationException("Payment must be authorized before capture");

            PaidAmount = Amount;
            Status = PaymentStatus.Paid;
            CapturedAt = DateTime.UtcNow;

            var transaction = Transaction.CreateCapture(
                Amount,
                transactionId ?? PaymentIntentId ?? "manual",
                $"Capture for Payment {Id}",
                Metadata);

            _transactions.Add(transaction);

            RaiseDomainEvent(new PaymentCapturedEvent(Id, BookingId, CustomerId, ProviderId, Amount, CapturedAt.Value));
        }

        /// <summary>
        /// Processes payment directly (charge without authorization)
        /// </summary>
        public void ProcessCharge(string paymentIntentId, string? paymentMethodId = null)
        {
            if (Status != PaymentStatus.Pending)
                throw new InvalidOperationException($"Cannot process payment in {Status} status");

            if (string.IsNullOrWhiteSpace(paymentIntentId))
                throw new ArgumentException("Payment intent ID is required", nameof(paymentIntentId));

            PaymentIntentId = paymentIntentId;
            PaymentMethodId = paymentMethodId;
            PaidAmount = Amount;
            Status = PaymentStatus.Paid;
            CapturedAt = DateTime.UtcNow;

            var transaction = Transaction.CreateCharge(
                Amount,
                paymentIntentId,
                $"Charge for Payment {Id}",
                Metadata);

            _transactions.Add(transaction);

            RaiseDomainEvent(new PaymentProcessedEvent(Id, BookingId, CustomerId, ProviderId, Amount, CapturedAt.Value));
        }

        /// <summary>
        /// Processes a partial payment
        /// </summary>
        public void ProcessPartialPayment(Money amount, string transactionId)
        {
            if (Status == PaymentStatus.Refunded)
                throw new InvalidOperationException("Cannot process payment for refunded payment");

            if (Status == PaymentStatus.Failed)
                throw new InvalidOperationException("Cannot process payment for failed payment");

            if (amount.Amount <= 0)
                throw new ArgumentException("Payment amount must be positive", nameof(amount));

            if (amount.Currency != Amount.Currency)
                throw new ArgumentException("Payment currency mismatch");

            var newPaidAmount = PaidAmount.Add(amount);
            if (newPaidAmount.Amount > Amount.Amount)
                throw new InvalidOperationException("Paid amount cannot exceed total amount");

            PaidAmount = newPaidAmount;
            Status = PaidAmount.Amount >= Amount.Amount ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;

            var transaction = Transaction.CreateCharge(
                amount,
                transactionId,
                $"Partial payment for Payment {Id}",
                Metadata);

            _transactions.Add(transaction);

            RaiseDomainEvent(new PaymentProcessedEvent(Id, BookingId, CustomerId, ProviderId, amount, DateTime.UtcNow));
        }

        /// <summary>
        /// Refunds the payment (full or partial)
        /// </summary>
        public void Refund(Money refundAmount, string refundId, RefundReason reason, string? notes = null)
        {
            if (Status != PaymentStatus.Paid && Status != PaymentStatus.PartiallyPaid && Status != PaymentStatus.PartiallyRefunded)
                throw new InvalidOperationException($"Cannot refund payment in {Status} status");

            if (refundAmount.Amount <= 0)
                throw new ArgumentException("Refund amount must be positive", nameof(refundAmount));

            if (refundAmount.Currency != Amount.Currency)
                throw new ArgumentException("Refund currency mismatch");

            var newRefundedAmount = RefundedAmount.Add(refundAmount);
            if (newRefundedAmount.Amount > PaidAmount.Amount)
                throw new InvalidOperationException("Refunded amount cannot exceed paid amount");

            RefundedAmount = newRefundedAmount;
            Status = RefundedAmount.Amount >= PaidAmount.Amount
                ? PaymentStatus.Refunded
                : PaymentStatus.PartiallyRefunded;
            RefundedAt = DateTime.UtcNow;

            var metadata = new Dictionary<string, string>(Metadata)
            {
                ["RefundReason"] = reason.ToString(),
                ["RefundNotes"] = notes ?? string.Empty
            };

            var transaction = Transaction.CreateRefund(
                refundAmount,
                refundId,
                $"Refund for Payment {Id}",
                metadata);

            _transactions.Add(transaction);

            RaiseDomainEvent(new PaymentRefundedEvent(
                Id,
                BookingId,
                CustomerId,
                ProviderId,
                refundAmount,
                reason,
                RefundedAt.Value));
        }

        /// <summary>
        /// Marks payment as failed
        /// </summary>
        public void MarkAsFailed(string reason)
        {
            if (Status == PaymentStatus.Paid || Status == PaymentStatus.Refunded)
                throw new InvalidOperationException($"Cannot mark {Status} payment as failed");

            Status = PaymentStatus.Failed;
            FailureReason = reason;
            FailedAt = DateTime.UtcNow;

            RaiseDomainEvent(new PaymentFailedEvent(Id, BookingId, CustomerId, reason, FailedAt.Value));
        }

        /// <summary>
        /// Gets the remaining amount to be paid
        /// </summary>
        public Money GetRemainingAmount()
        {
            return Amount.Subtract(PaidAmount);
        }

        /// <summary>
        /// Gets the refundable amount
        /// </summary>
        public Money GetRefundableAmount()
        {
            return PaidAmount.Subtract(RefundedAmount);
        }

        /// <summary>
        /// Checks if payment is fully paid
        /// </summary>
        public bool IsFullyPaid()
        {
            return PaidAmount.Amount >= Amount.Amount;
        }

        /// <summary>
        /// Checks if payment can be refunded
        /// </summary>
        public bool CanBeRefunded()
        {
            return (Status == PaymentStatus.Paid || Status == PaymentStatus.PartiallyRefunded)
                   && GetRefundableAmount().Amount > 0;
        }
    }
}

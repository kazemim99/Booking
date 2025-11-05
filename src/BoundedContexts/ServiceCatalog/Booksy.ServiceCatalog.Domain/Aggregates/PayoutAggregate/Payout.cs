// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/PayoutAggregate/Payout.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.PayoutAggregate
{
    /// <summary>
    /// Payout aggregate root - manages provider payouts
    /// </summary>
    public sealed class Payout : AggregateRoot<PayoutId>
    {
        private readonly List<PaymentId> _paymentIds = new();

        // Identity
        public ProviderId ProviderId { get; private set; }

        // Payout Details
        public Money GrossAmount { get; private set; }
        public Money CommissionAmount { get; private set; }
        public Money NetAmount { get; private set; }
        public PayoutStatus Status { get; private set; }

        // Period
        public DateTime PeriodStart { get; private set; }
        public DateTime PeriodEnd { get; private set; }

        // Bank Details
        public string? BankAccountLast4 { get; private set; }
        public string? BankName { get; private set; }

        // External Provider Details
        public string? ExternalPayoutId { get; private set; }
        public string? StripeAccountId { get; private set; }

        // Timestamps
        public DateTime CreatedAt { get; private set; }
        public DateTime? ScheduledAt { get; private set; }
        public DateTime? PaidAt { get; private set; }
        public DateTime? FailedAt { get; private set; }
        public DateTime? CancelledAt { get; private set; }

        // Additional Info
        public string? FailureReason { get; private set; }
        public string? Notes { get; private set; }
        public Dictionary<string, string> Metadata { get; private set; }

        // Collections
        public IReadOnlyList<PaymentId> PaymentIds => _paymentIds.AsReadOnly();

        // EF Core requires parameterless constructor
        private Payout()
        {
            Metadata = new Dictionary<string, string>();
        }

        private Payout(
            PayoutId id,
            ProviderId providerId,
            Money grossAmount,
            Money commissionAmount,
            DateTime periodStart,
            DateTime periodEnd,
            IEnumerable<PaymentId> paymentIds,
            string? notes,
            Dictionary<string, string>? metadata) : base(id)
        {
            ProviderId = providerId ?? throw new ArgumentNullException(nameof(providerId));
            GrossAmount = grossAmount ?? throw new ArgumentNullException(nameof(grossAmount));
            CommissionAmount = commissionAmount ?? throw new ArgumentNullException(nameof(commissionAmount));

            if (commissionAmount.Amount > grossAmount.Amount)
                throw new InvalidOperationException("Commission cannot exceed gross amount");

            NetAmount = grossAmount.Subtract(commissionAmount);

            if (NetAmount.Amount <= 0)
                throw new InvalidOperationException("Net payout amount must be positive");

            PeriodStart = periodStart;
            PeriodEnd = periodEnd;

            if (periodEnd <= periodStart)
                throw new ArgumentException("Period end must be after period start");

            _paymentIds.AddRange(paymentIds ?? Enumerable.Empty<PaymentId>());

            Status = PayoutStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            Notes = notes;
            Metadata = metadata ?? new Dictionary<string, string>();

            RaiseDomainEvent(new PayoutCreatedEvent(id, providerId, NetAmount, periodStart, periodEnd, CreatedAt));
        }

        /// <summary>
        /// Creates a new payout for a provider
        /// </summary>
        public static Payout Create(
            ProviderId providerId,
            Money grossAmount,
            Money commissionAmount,
            DateTime periodStart,
            DateTime periodEnd,
            IEnumerable<PaymentId> paymentIds,
            string? notes = null,
            Dictionary<string, string>? metadata = null)
        {
            return new Payout(
                PayoutId.New(),
                providerId,
                grossAmount,
                commissionAmount,
                periodStart,
                periodEnd,
                paymentIds,
                notes,
                metadata);
        }

        /// <summary>
        /// Schedules the payout for a specific date
        /// </summary>
        public void Schedule(DateTime scheduledDate)
        {
            if (Status != PayoutStatus.Pending)
                throw new InvalidOperationException($"Cannot schedule payout in {Status} status");

            if (scheduledDate <= DateTime.UtcNow)
                throw new ArgumentException("Scheduled date must be in the future", nameof(scheduledDate));

            ScheduledAt = scheduledDate;

            RaiseDomainEvent(new PayoutScheduledEvent(Id, ProviderId, ScheduledAt.Value));
        }

        /// <summary>
        /// Marks payout as processing
        /// </summary>
        public void MarkAsProcessing(string externalPayoutId, string? stripeAccountId = null)
        {
            if (Status != PayoutStatus.Pending)
                throw new InvalidOperationException($"Cannot process payout in {Status} status");

            if (string.IsNullOrWhiteSpace(externalPayoutId))
                throw new ArgumentException("External payout ID is required", nameof(externalPayoutId));

            Status = PayoutStatus.Processing;
            ExternalPayoutId = externalPayoutId;
            StripeAccountId = stripeAccountId;

            RaiseDomainEvent(new PayoutProcessingEvent(Id, ProviderId, externalPayoutId, DateTime.UtcNow));
        }

        /// <summary>
        /// Marks payout as paid
        /// </summary>
        public void MarkAsPaid(string? bankAccountLast4 = null, string? bankName = null)
        {
            if (Status != PayoutStatus.Processing)
                throw new InvalidOperationException($"Cannot mark payout as paid from {Status} status");

            Status = PayoutStatus.Paid;
            PaidAt = DateTime.UtcNow;
            BankAccountLast4 = bankAccountLast4;
            BankName = bankName;

            RaiseDomainEvent(new PayoutCompletedEvent(Id, ProviderId, NetAmount, PaidAt.Value));
        }

        /// <summary>
        /// Marks payout as failed
        /// </summary>
        public void MarkAsFailed(string reason)
        {
            if (Status == PayoutStatus.Paid)
                throw new InvalidOperationException("Cannot mark paid payout as failed");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Failure reason is required", nameof(reason));

            Status = PayoutStatus.Failed;
            FailureReason = reason;
            FailedAt = DateTime.UtcNow;

            RaiseDomainEvent(new PayoutFailedEvent(Id, ProviderId, reason, FailedAt.Value));
        }

        /// <summary>
        /// Cancels the payout
        /// </summary>
        public void Cancel(string reason)
        {
            if (Status == PayoutStatus.Paid)
                throw new InvalidOperationException("Cannot cancel paid payout");

            if (Status == PayoutStatus.Processing)
                throw new InvalidOperationException("Cannot cancel payout that is being processed");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Cancellation reason is required", nameof(reason));

            Status = PayoutStatus.Cancelled;
            FailureReason = reason;
            CancelledAt = DateTime.UtcNow;

            RaiseDomainEvent(new PayoutCancelledEvent(Id, ProviderId, reason, CancelledAt.Value));
        }

        /// <summary>
        /// Puts payout on hold
        /// </summary>
        public void PutOnHold(string reason)
        {
            if (Status == PayoutStatus.Paid)
                throw new InvalidOperationException("Cannot put paid payout on hold");

            if (Status == PayoutStatus.Cancelled)
                throw new InvalidOperationException("Cannot put cancelled payout on hold");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Hold reason is required", nameof(reason));

            Status = PayoutStatus.OnHold;
            Notes = reason;

            RaiseDomainEvent(new PayoutOnHoldEvent(Id, ProviderId, reason, DateTime.UtcNow));
        }

        /// <summary>
        /// Releases payout from hold
        /// </summary>
        public void ReleaseFromHold()
        {
            if (Status != PayoutStatus.OnHold)
                throw new InvalidOperationException("Payout is not on hold");

            Status = PayoutStatus.Pending;

            RaiseDomainEvent(new PayoutReleasedFromHoldEvent(Id, ProviderId, DateTime.UtcNow));
        }

        /// <summary>
        /// Calculates the commission percentage
        /// </summary>
        public decimal GetCommissionPercentage()
        {
            if (GrossAmount.Amount == 0)
                return 0;

            return (CommissionAmount.Amount / GrossAmount.Amount) * 100;
        }
    }
}

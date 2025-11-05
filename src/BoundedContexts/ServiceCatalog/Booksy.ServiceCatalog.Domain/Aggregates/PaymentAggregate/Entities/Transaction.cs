// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/PaymentAggregate/Entities/Transaction.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate.Entities
{
    /// <summary>
    /// Represents an individual financial transaction within a payment
    /// </summary>
    public sealed class Transaction : Entity<Guid>
    {
        public TransactionType Type { get; private set; }
        public Money Amount { get; private set; }
        public string? ExternalTransactionId { get; private set; }
        public string? Reference { get; private set; }
        public string Status { get; private set; }
        public string? StatusReason { get; private set; }
        public DateTime ProcessedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public Dictionary<string, string> Metadata { get; private set; }

        // EF Core requires parameterless constructor
        private Transaction() : base(Guid.NewGuid())
        {
            Metadata = new Dictionary<string, string>();
            Status = string.Empty;
        }

        private Transaction(
            TransactionType type,
            Money amount,
            string? externalTransactionId,
            string? reference,
            string status,
            DateTime processedAt,
            Dictionary<string, string>? metadata = null) : base(Guid.NewGuid())
        {
            Type = type;
            Amount = amount ?? throw new ArgumentNullException(nameof(amount));
            ExternalTransactionId = externalTransactionId;
            Reference = reference;
            Status = status ?? throw new ArgumentNullException(nameof(status));
            ProcessedAt = processedAt;
            Metadata = metadata ?? new Dictionary<string, string>();
        }

        internal static Transaction CreateCharge(
            Money amount,
            string externalTransactionId,
            string? reference = null,
            Dictionary<string, string>? metadata = null)
        {
            return new Transaction(
                TransactionType.Charge,
                amount,
                externalTransactionId,
                reference,
                "succeeded",
                DateTime.UtcNow,
                metadata);
        }

        internal static Transaction CreateAuthorization(
            Money amount,
            string externalTransactionId,
            string? reference = null,
            Dictionary<string, string>? metadata = null)
        {
            return new Transaction(
                TransactionType.Authorization,
                amount,
                externalTransactionId,
                reference,
                "authorized",
                DateTime.UtcNow,
                metadata);
        }

        internal static Transaction CreateCapture(
            Money amount,
            string externalTransactionId,
            string? reference = null,
            Dictionary<string, string>? metadata = null)
        {
            return new Transaction(
                TransactionType.Capture,
                amount,
                externalTransactionId,
                reference,
                "captured",
                DateTime.UtcNow,
                metadata);
        }

        internal static Transaction CreateRefund(
            Money amount,
            string externalTransactionId,
            string? reference = null,
            Dictionary<string, string>? metadata = null)
        {
            return new Transaction(
                TransactionType.Refund,
                amount,
                externalTransactionId,
                reference,
                "refunded",
                DateTime.UtcNow,
                metadata);
        }

        internal static Transaction CreatePayout(
            Money amount,
            string externalTransactionId,
            string? reference = null,
            Dictionary<string, string>? metadata = null)
        {
            return new Transaction(
                TransactionType.Payout,
                amount,
                externalTransactionId,
                reference,
                "paid",
                DateTime.UtcNow,
                metadata);
        }

        internal void MarkAsCompleted(string? reason = null)
        {
            CompletedAt = DateTime.UtcNow;
            if (reason != null)
            {
                StatusReason = reason;
            }
        }

        internal void MarkAsFailed(string reason)
        {
            Status = "failed";
            StatusReason = reason;
            CompletedAt = DateTime.UtcNow;
        }

        internal void UpdateStatus(string status, string? reason = null)
        {
            Status = status ?? throw new ArgumentNullException(nameof(status));
            if (reason != null)
            {
                StatusReason = reason;
            }
        }
    }
}

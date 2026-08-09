using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.LedgerAggregate
{
    /// <summary>
    /// An immutable, append-only line in the platform's double-entry ledger. Entries are never updated or deleted;
    /// corrections are made by posting further (reversing) entries. Each entry belongs to a
    /// <see cref="LedgerTransaction"/> identified by <see cref="EventId"/>; the entries of one transaction always
    /// sum to zero (see <see cref="SignedAmount"/>).
    /// </summary>
    public sealed class LedgerEntry : Entity<Guid>
    {
        /// <summary>The money event (payment/refund/payout domain event id) that produced this entry. Used to make
        /// posting idempotent — redelivery of the same event must not double-post (DB unique <c>(EventId, Account)</c>).</summary>
        public Guid EventId { get; private set; }

        public LedgerAccount Account { get; private set; }
        public LedgerDirection Direction { get; private set; }
        public LedgerEntryType EntryType { get; private set; }

        /// <summary>The entry amount, always non-negative. The sign is carried by <see cref="Direction"/>.</summary>
        public Money Amount { get; private set; }

        // Correlation (nullable — a payout has no booking/payment; a charge has all three).
        public Guid? BookingId { get; private set; }
        public Guid? PaymentId { get; private set; }
        public Guid? ProviderId { get; private set; }

        public DateTime PostedAt { get; private set; }

        private LedgerEntry() { } // EF

        /// <summary>
        /// Prefer the <see cref="LedgerTransaction"/> factories, which guarantee a balanced double-entry set.
        /// This constructor is public only so a balanced set can be assembled explicitly (and unit-tested).
        /// </summary>
        public LedgerEntry(
            Guid eventId,
            LedgerAccount account,
            LedgerDirection direction,
            LedgerEntryType entryType,
            Money amount,
            Guid? bookingId,
            Guid? paymentId,
            Guid? providerId)
            : base(Guid.NewGuid())
        {
            if (amount is null) throw new ArgumentNullException(nameof(amount));
            if (amount.Amount < 0) throw new ArgumentException("Ledger entry amount cannot be negative; use Direction to express sign.", nameof(amount));

            EventId = eventId;
            Account = account;
            Direction = direction;
            EntryType = entryType;
            Amount = amount;
            BookingId = bookingId;
            PaymentId = paymentId;
            ProviderId = providerId;
            PostedAt = DateTime.UtcNow;
        }

        /// <summary>The entry amount as a signed decimal: positive for a debit, negative for a credit. The signed
        /// amounts of a balanced <see cref="LedgerTransaction"/> sum to exactly zero.</summary>
        public decimal SignedAmount => Direction == LedgerDirection.Debit ? Amount.Amount : -Amount.Amount;
    }
}

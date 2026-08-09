using Booksy.Core.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.LedgerAggregate
{
    /// <summary>
    /// A balanced set of <see cref="LedgerEntry"/> produced by a single money event. Construction enforces the
    /// double-entry invariant: all entries share one currency and their signed amounts sum to exactly zero. Callers
    /// use the factory methods (<see cref="ForCharge"/>, <see cref="ForRefund"/>, <see cref="ForPayout"/>) which
    /// build correct, balanced entries; the constructor validates any set as a safety net.
    /// </summary>
    public sealed class LedgerTransaction
    {
        public Guid EventId { get; }
        public IReadOnlyList<LedgerEntry> Entries { get; }

        public LedgerTransaction(Guid eventId, IReadOnlyList<LedgerEntry> entries)
        {
            if (eventId == Guid.Empty) throw new ArgumentException("EventId is required.", nameof(eventId));
            if (entries is null || entries.Count < 2)
                throw new ArgumentException("A ledger transaction needs at least two entries (double-entry).", nameof(entries));
            if (entries.Any(e => e.EventId != eventId))
                throw new ArgumentException("All entries must share the transaction's EventId.", nameof(entries));

            var currencies = entries.Select(e => e.Amount.Currency).Distinct().ToArray();
            if (currencies.Length != 1)
                throw new InvalidOperationException($"A ledger transaction must be single-currency, found: {string.Join(",", currencies)}.");

            var sum = entries.Sum(e => e.SignedAmount);
            if (sum != 0m)
                throw new InvalidOperationException($"Ledger transaction is not balanced; signed amounts sum to {sum}, expected 0.");

            EventId = eventId;
            Entries = entries;
        }

        /// <summary>
        /// A customer charge: gateway receives <paramref name="gross"/>; the platform books <paramref name="commission"/>
        /// as revenue and the remainder as owed to the provider.
        /// </summary>
        public static LedgerTransaction ForCharge(
            Guid eventId, Guid? bookingId, Guid paymentId, Guid providerId, Money gross, Money? commission = null)
        {
            commission ??= Money.Zero(gross.Currency);
            EnsureSameCurrency(gross, commission);
            if (commission.Amount > gross.Amount)
                throw new InvalidOperationException("Commission cannot exceed the gross charge.");

            var providerShare = Money.Create(gross.Amount - commission.Amount, gross.Currency);
            var entries = new List<LedgerEntry>
            {
                new(eventId, LedgerAccount.GatewayClearing, LedgerDirection.Debit, LedgerEntryType.Charge, gross, bookingId, paymentId, providerId),
                new(eventId, LedgerAccount.ProviderPayable, LedgerDirection.Credit, LedgerEntryType.Charge, providerShare, bookingId, paymentId, providerId),
            };
            // Commission is usually recognized at payout, not at charge; only post a revenue entry if a
            // non-zero commission is supplied at charge time.
            if (commission.Amount > 0)
                entries.Add(new(eventId, LedgerAccount.PlatformRevenue, LedgerDirection.Credit, LedgerEntryType.Commission, commission, bookingId, paymentId, providerId));

            return new LedgerTransaction(eventId, entries);
        }

        /// <summary>
        /// A refund of <paramref name="refund"/>: gateway pays out, reversing <paramref name="commissionReversed"/> of
        /// platform revenue and the remainder from the provider's payable balance.
        /// </summary>
        public static LedgerTransaction ForRefund(
            Guid eventId, Guid? bookingId, Guid paymentId, Guid providerId, Money refund, Money? commissionReversed = null)
        {
            commissionReversed ??= Money.Zero(refund.Currency);
            EnsureSameCurrency(refund, commissionReversed);
            if (commissionReversed.Amount > refund.Amount)
                throw new InvalidOperationException("Reversed commission cannot exceed the refund.");

            var providerReversal = Money.Create(refund.Amount - commissionReversed.Amount, refund.Currency);
            var entries = new List<LedgerEntry>
            {
                new(eventId, LedgerAccount.GatewayClearing, LedgerDirection.Credit, LedgerEntryType.Refund, refund, bookingId, paymentId, providerId),
                new(eventId, LedgerAccount.ProviderPayable, LedgerDirection.Debit, LedgerEntryType.Refund, providerReversal, bookingId, paymentId, providerId),
            };
            if (commissionReversed.Amount > 0)
                entries.Add(new(eventId, LedgerAccount.PlatformRevenue, LedgerDirection.Debit, LedgerEntryType.Commission, commissionReversed, bookingId, paymentId, providerId));

            return new LedgerTransaction(eventId, entries);
        }

        /// <summary>
        /// A payout to a provider: clears the provider-payable liability for <paramref name="gross"/>, recognizes the
        /// platform's <paramref name="commission"/> as revenue, and pays the net (gross − commission) out of gateway
        /// cash. With no commission it is a straight 2-entry transfer (ProviderPayable → GatewayClearing).
        /// </summary>
        public static LedgerTransaction ForPayout(Guid eventId, Guid providerId, Money gross, Money? commission = null)
        {
            commission ??= Money.Zero(gross.Currency);
            EnsureSameCurrency(gross, commission);
            if (commission.Amount > gross.Amount)
                throw new InvalidOperationException("Payout commission cannot exceed the gross.");

            var net = Money.Create(gross.Amount - commission.Amount, gross.Currency);
            var entries = new List<LedgerEntry>
            {
                new(eventId, LedgerAccount.ProviderPayable, LedgerDirection.Debit, LedgerEntryType.Payout, gross, null, null, providerId),
                new(eventId, LedgerAccount.GatewayClearing, LedgerDirection.Credit, LedgerEntryType.Payout, net, null, null, providerId),
            };
            if (commission.Amount > 0)
                entries.Add(new(eventId, LedgerAccount.PlatformRevenue, LedgerDirection.Credit, LedgerEntryType.Commission, commission, null, null, providerId));

            return new LedgerTransaction(eventId, entries);
        }

        private static void EnsureSameCurrency(Money a, Money b)
        {
            if (a.Currency != b.Currency)
                throw new InvalidOperationException($"Currency mismatch: {a.Currency} vs {b.Currency}.");
        }
    }
}

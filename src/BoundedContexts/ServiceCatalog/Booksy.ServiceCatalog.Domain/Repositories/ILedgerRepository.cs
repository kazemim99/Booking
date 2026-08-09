using Booksy.ServiceCatalog.Domain.Aggregates.LedgerAggregate;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    /// <summary>
    /// Append-only access to the double-entry ledger. Entries are never updated or deleted — corrections are made by
    /// posting a compensating <see cref="LedgerTransaction"/>. Posting is idempotent by event id.
    /// </summary>
    public interface ILedgerRepository
    {
        /// <summary>True if any entry for <paramref name="eventId"/> has already been posted.</summary>
        Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Appends the transaction's balanced entries to the current unit of work if — and only if — that event has
        /// not already been posted. Idempotent: a second call for the same <see cref="LedgerTransaction.EventId"/> is
        /// a no-op. Does not save; the caller's unit of work commits (so a charge and its ledger entries are atomic).
        /// </summary>
        Task<bool> AppendAsync(LedgerTransaction transaction, CancellationToken cancellationToken = default);

        /// <summary>Signed balance of an account (Σ debit − credit), optionally scoped to a provider.</summary>
        Task<decimal> GetAccountBalanceAsync(LedgerAccount account, Guid? providerId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// The amount currently owed to a provider, derived <b>only</b> from the ledger — the single source of truth
        /// for settlements. Equals credits (charges) − debits (payouts + refund reversals) on the ProviderPayable
        /// account. Positive = owed; zero/negative = nothing to pay (e.g. refunds exceeded unpaid charges).
        /// </summary>
        Task<decimal> GetProviderPayableBalanceAsync(Guid providerId, CancellationToken cancellationToken = default);

        /// <summary>All entries recorded for a payment (charge + any refunds) — for audit/traceability.</summary>
        Task<IReadOnlyList<LedgerEntry>> GetByPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);

        /// <summary>Distinct payment ids that posted a charge for this provider — audit trail of what a payout covers.</summary>
        Task<IReadOnlyList<Guid>> GetProviderChargePaymentIdsAsync(Guid providerId, CancellationToken cancellationToken = default);

        /// <summary>The signed balance of every account (Σ over all providers) — for balance-drift detection.</summary>
        Task<IReadOnlyDictionary<LedgerAccount, decimal>> GetAllAccountBalancesAsync(CancellationToken cancellationToken = default);
    }
}

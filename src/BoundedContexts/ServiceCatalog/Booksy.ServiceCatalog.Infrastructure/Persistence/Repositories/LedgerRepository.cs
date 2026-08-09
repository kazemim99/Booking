using Booksy.ServiceCatalog.Domain.Aggregates.LedgerAggregate;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Append-only ledger store. Never updates or deletes entries (the DbContext enforces this); posting is
    /// idempotent by event id, backed by the unique <c>(EventId, Account)</c> index.
    /// </summary>
    public sealed class LedgerRepository : ILedgerRepository
    {
        private readonly ServiceCatalogDbContext _context;

        public LedgerRepository(ServiceCatalogDbContext context) => _context = context;

        public Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            _context.LedgerEntries.AnyAsync(e => e.EventId == eventId, cancellationToken);

        public async Task<bool> AppendAsync(LedgerTransaction transaction, CancellationToken cancellationToken = default)
        {
            // Idempotent: if this event was already posted, do nothing. The unique (EventId, Account) index is the
            // hard backstop against a race; this check keeps the common replay path from failing the whole commit.
            if (await ExistsAsync(transaction.EventId, cancellationToken))
                return false;

            await _context.LedgerEntries.AddRangeAsync(transaction.Entries, cancellationToken);
            return true;
        }

        public async Task<decimal> GetAccountBalanceAsync(
            LedgerAccount account, Guid? providerId = null, CancellationToken cancellationToken = default)
        {
            var q = _context.LedgerEntries.AsNoTracking().Where(e => e.Account == account);
            if (providerId.HasValue)
                q = q.Where(e => e.ProviderId == providerId.Value);

            var debit = await q.Where(e => e.Direction == LedgerDirection.Debit).SumAsync(e => e.Amount.Amount, cancellationToken);
            var credit = await q.Where(e => e.Direction == LedgerDirection.Credit).SumAsync(e => e.Amount.Amount, cancellationToken);
            return debit - credit;
        }

        public async Task<decimal> GetProviderPayableBalanceAsync(Guid providerId, CancellationToken cancellationToken = default)
        {
            // ProviderPayable is credit-normal: amount owed = credits − debits = −(signed balance).
            var signed = await GetAccountBalanceAsync(LedgerAccount.ProviderPayable, providerId, cancellationToken);
            return -signed;
        }

        public async Task<IReadOnlyList<LedgerEntry>> GetByPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default) =>
            await _context.LedgerEntries.AsNoTracking()
                .Where(e => e.PaymentId == paymentId)
                .OrderBy(e => e.PostedAt)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Guid>> GetProviderChargePaymentIdsAsync(Guid providerId, CancellationToken cancellationToken = default) =>
            await _context.LedgerEntries.AsNoTracking()
                .Where(e => e.ProviderId == providerId && e.EntryType == LedgerEntryType.Charge && e.PaymentId != null)
                .Select(e => e.PaymentId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyDictionary<LedgerAccount, decimal>> GetAllAccountBalancesAsync(CancellationToken cancellationToken = default)
        {
            var rows = await _context.LedgerEntries.AsNoTracking()
                .GroupBy(e => new { e.Account, e.Direction })
                .Select(g => new { g.Key.Account, g.Key.Direction, Sum = g.Sum(x => x.Amount.Amount) })
                .ToListAsync(cancellationToken);

            var result = new Dictionary<LedgerAccount, decimal>();
            foreach (var r in rows)
            {
                var signed = r.Direction == LedgerDirection.Debit ? r.Sum : -r.Sum;
                result[r.Account] = result.GetValueOrDefault(r.Account) + signed;
            }
            return result;
        }
    }
}

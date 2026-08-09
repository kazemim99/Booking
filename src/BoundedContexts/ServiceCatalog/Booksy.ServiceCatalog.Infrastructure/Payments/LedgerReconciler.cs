using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.LedgerAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Payments
{
    /// <summary>
    /// C5 reconciliation after partial failure. A payment can be marked Paid while its ledger charge entry never got
    /// written (process crash between the payment commit and the ledger post, or an event-handler failure). This
    /// sweep finds captured payments that have no charge entry and posts it — idempotently, so re-runs and a
    /// concurrently-arriving handler never double-post. It closes any gap between payments and the ledger.
    /// </summary>
    public interface ILedgerReconciler
    {
        Task<int> ReconcileMissingChargeEntriesAsync(int batchSize = 200, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies the double-entry invariant across the whole ledger: the signed amounts of every entry must sum to
        /// exactly zero. A non-zero result means the ledger has drifted (a corruption or a bug in posting) and is
        /// logged as an error/alert. Returns the drift amount (0 = healthy).
        /// </summary>
        Task<decimal> DetectBalanceDriftAsync(CancellationToken cancellationToken = default);
    }

    public sealed class LedgerReconciler : ILedgerReconciler
    {
        private readonly ServiceCatalogDbContext _db;
        private readonly ILedgerRepository _ledger;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<LedgerReconciler> _logger;

        public LedgerReconciler(
            ServiceCatalogDbContext db, ILedgerRepository ledger, IServiceCatalogUnitOfWork unitOfWork, ILogger<LedgerReconciler> logger)
        {
            _db = db;
            _ledger = ledger;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> ReconcileMissingChargeEntriesAsync(int batchSize = 200, CancellationToken cancellationToken = default)
        {
            var captured = await _db.Payments.AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.PartiallyPaid)
                .OrderByDescending(p => p.CreatedAt)
                .Take(batchSize)
                .Select(p => new
                {
                    PaymentId = p.Id.Value,
                    BookingId = p.BookingId != null ? (Guid?)p.BookingId.Value : null,
                    ProviderId = p.ProviderId.Value,
                    Amount = p.PaidAmount.Amount,
                    Currency = p.PaidAmount.Currency
                })
                .ToListAsync(cancellationToken);

            var posted = 0;
            foreach (var p in captured)
            {
                var eventId = LedgerEventKeys.Charge(p.PaymentId);
                if (await _ledger.ExistsAsync(eventId, cancellationToken))
                    continue; // already recorded — idempotent

                var tx = LedgerTransaction.ForCharge(
                    eventId, p.BookingId, p.PaymentId, p.ProviderId,
                    Booksy.Core.Domain.ValueObjects.Money.Create(p.Amount, p.Currency));

                if (await _ledger.AppendAsync(tx, cancellationToken))
                    posted++;
            }

            if (posted > 0)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
                _logger.LogWarning("Ledger reconciliation posted {Count} missing charge entrie(s)", posted);
            }

            return posted;
        }

        public async Task<decimal> DetectBalanceDriftAsync(CancellationToken cancellationToken = default)
        {
            var balances = await _ledger.GetAllAccountBalancesAsync(cancellationToken);
            var drift = balances.Values.Sum();

            if (drift != 0m)
            {
                _logger.LogError(
                    "LEDGER DRIFT DETECTED: the double-entry ledger does not balance (Σ = {Drift}). Account balances: {Balances}",
                    drift, string.Join(", ", balances.Select(kv => $"{kv.Key}={kv.Value}")));
            }
            else
            {
                _logger.LogDebug("Ledger balance check OK (Σ = 0). Accounts: {Balances}",
                    string.Join(", ", balances.Select(kv => $"{kv.Key}={kv.Value}")));
            }

            return drift;
        }
    }
}

using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.Infrastructure.External.Payment.ZarinPal;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Payments
{
    /// <summary>
    /// Reconciles stale <see cref="PaymentStatus.Pending"/> payments (C2, invariants
    /// I1/I3 — money never lost or untraceable). A payment can get stuck in Pending when
    /// the ZarinPal callback is lost, the process crashes between charge and confirm, or a
    /// commit fails after the gateway moved money. This sweep queries the gateway as a
    /// read-only oracle and converges the DB (the record of truth) to the gateway's actual
    /// state: verified → Paid, not-verified → Failed. Idempotent: an already-Paid payment
    /// is skipped, and re-runs are safe.
    /// </summary>
    public interface IPaymentReconciler
    {
        Task<int> ReconcileStalePendingAsync(TimeSpan olderThan, int batchSize = 100, CancellationToken cancellationToken = default);
    }

    /// <inheritdoc />
    public sealed class PaymentReconciler : IPaymentReconciler
    {
        private readonly ServiceCatalogDbContext _db;
        private readonly IZarinPalService _zarinPal;
        private readonly IPaymentWriteRepository _payments;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentReconciler> _logger;

        public PaymentReconciler(
            ServiceCatalogDbContext db,
            IZarinPalService zarinPal,
            IPaymentWriteRepository payments,
            IServiceCatalogUnitOfWork unitOfWork,
            ILogger<PaymentReconciler> logger)
        {
            _db = db;
            _zarinPal = zarinPal;
            _payments = payments;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> ReconcileStalePendingAsync(
            TimeSpan olderThan, int batchSize = 100, CancellationToken cancellationToken = default)
        {
            var minutes = Math.Max(0, (int)olderThan.TotalMinutes);

            // Candidate authorities selected at the SQL layer (robust against enum
            // value-converter translation); staleness is measured against the DB clock
            // (avoids app/DB skew); each aggregate is then loaded via the proven
            // GetByAuthorityAsync path.
            var authorities = await _db.Database
                .SqlQueryRaw<string>(
                    @"SELECT ""Authority"" AS ""Value"" FROM ""ServiceCatalog"".""Payments""
                      WHERE ""Status"" = 'Pending' AND ""Authority"" IS NOT NULL
                        AND ""CreatedAt"" < now() - make_interval(mins => {0})
                      ORDER BY ""CreatedAt"" LIMIT {1}", minutes, batchSize)
                .ToListAsync(cancellationToken);

            if (authorities.Count == 0)
                return 0;

            _logger.LogInformation("Reconciling {Count} stale pending payment(s)", authorities.Count);

            var reconciled = 0;
            foreach (var authority in authorities)
            {
                try
                {
                    var payment = await _payments.GetByAuthorityAsync(authority, cancellationToken);
                    if (payment is null || payment.Status != PaymentStatus.Pending)
                        continue; // already resolved by a concurrent callback — idempotent

                    // Gateway is a read-only oracle: ask ZarinPal what really happened.
                    var result = await _zarinPal.VerifyPaymentAsync(
                        authority, payment.Amount.Amount, cancellationToken);

                    if (result.IsSuccessful)
                    {
                        payment.VerifyPayment(result.RefId.ToString(), result.CardPan, result.Fee);
                        _logger.LogInformation(
                            "Reconciled payment {PaymentId} → Paid (RefId {RefId})", payment.Id.Value, result.RefId);
                    }
                    else
                    {
                        payment.MarkPaymentRequestAsFailed(
                            result.ErrorCode.ToString(),
                            result.ErrorMessage ?? "Reconciliation: payment not verified by gateway");
                        _logger.LogInformation(
                            "Reconciled payment {PaymentId} → Failed ({Code})", payment.Id.Value, result.ErrorCode);
                    }

                    await _unitOfWork.CommitAsync(cancellationToken);
                    reconciled++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Reconciliation failed for authority {Authority}; leaving Pending for the next sweep", authority);
                }
            }

            return reconciled;
        }
    }
}

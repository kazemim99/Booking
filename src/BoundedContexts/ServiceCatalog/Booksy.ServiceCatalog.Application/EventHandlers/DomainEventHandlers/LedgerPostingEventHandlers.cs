using Booksy.Core.Application.Abstractions.Events;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.LedgerAggregate;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.DomainEventHandlers
{
    /// <summary>
    /// C5 financial-ledger. These handlers append balanced, immutable double-entry records for every money movement.
    /// The domain-event dispatcher runs handlers in their own DI scope (their own DbContext), so each handler commits
    /// its own ledger write — the ledger is intentionally <b>decoupled</b> from the command's transaction (design D2).
    /// Posting is idempotent by a stable event id (→ unique <c>(EventId, Account)</c>), so replays and duplicate
    /// deliveries are no-ops, and the ledger reconciler closes any gap left by a partial failure.
    /// </summary>
    public sealed class PostChargeToLedgerOnPaymentVerified : IDomainEventHandler<PaymentVerifiedEvent>
    {
        private readonly ILedgerRepository _ledger;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<PostChargeToLedgerOnPaymentVerified> _logger;

        public PostChargeToLedgerOnPaymentVerified(ILedgerRepository ledger, IServiceCatalogUnitOfWork unitOfWork, ILogger<PostChargeToLedgerOnPaymentVerified> logger)
        {
            _ledger = ledger;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleAsync(PaymentVerifiedEvent e, CancellationToken cancellationToken)
        {
            var eventId = LedgerEventKeys.Charge(e.PaymentId.Value);
            var tx = LedgerTransaction.ForCharge(eventId, e.BookingId?.Value, e.PaymentId.Value, e.ProviderId.Value, e.Amount);
            if (await _ledger.AppendAsync(tx, cancellationToken))
            {
                await _unitOfWork.CommitAsync(cancellationToken);
                _logger.LogInformation("Ledger charge posted for payment {PaymentId} (verified)", e.PaymentId.Value);
            }
        }
    }

    public sealed class PostChargeToLedgerOnPaymentProcessed : IDomainEventHandler<PaymentProcessedEvent>
    {
        private readonly ILedgerRepository _ledger;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<PostChargeToLedgerOnPaymentProcessed> _logger;

        public PostChargeToLedgerOnPaymentProcessed(ILedgerRepository ledger, IServiceCatalogUnitOfWork unitOfWork, ILogger<PostChargeToLedgerOnPaymentProcessed> logger)
        {
            _ledger = ledger;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleAsync(PaymentProcessedEvent e, CancellationToken cancellationToken)
        {
            var eventId = LedgerEventKeys.Charge(e.PaymentId.Value);
            var tx = LedgerTransaction.ForCharge(eventId, e.BookingId?.Value, e.PaymentId.Value, e.ProviderId.Value, e.Amount);
            if (await _ledger.AppendAsync(tx, cancellationToken))
            {
                await _unitOfWork.CommitAsync(cancellationToken);
                _logger.LogInformation("Ledger charge posted for payment {PaymentId} (processed)", e.PaymentId.Value);
            }
        }
    }

    public sealed class PostRefundToLedgerOnPaymentRefunded : IDomainEventHandler<PaymentRefundedEvent>
    {
        private readonly ILedgerRepository _ledger;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<PostRefundToLedgerOnPaymentRefunded> _logger;

        public PostRefundToLedgerOnPaymentRefunded(ILedgerRepository ledger, IServiceCatalogUnitOfWork unitOfWork, ILogger<PostRefundToLedgerOnPaymentRefunded> logger)
        {
            _ledger = ledger;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleAsync(PaymentRefundedEvent e, CancellationToken cancellationToken)
        {
            var eventId = LedgerEventKeys.Refund(e.PaymentId.Value, e.RefundedAt);
            var tx = LedgerTransaction.ForRefund(eventId, e.BookingId?.Value, e.PaymentId.Value, e.ProviderId.Value, e.RefundAmount);
            if (await _ledger.AppendAsync(tx, cancellationToken))
            {
                await _unitOfWork.CommitAsync(cancellationToken);
                _logger.LogInformation("Ledger refund posted for payment {PaymentId}", e.PaymentId.Value);
            }
        }
    }

    /// <summary>Settlement: when a payout completes, clear the provider-payable liability, recognize commission as
    /// platform revenue, and pay the net out of gateway cash. Idempotent by payout id.</summary>
    public sealed class PostPayoutToLedgerOnPayoutCompleted : IDomainEventHandler<PayoutCompletedEvent>
    {
        private readonly ILedgerRepository _ledger;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<PostPayoutToLedgerOnPayoutCompleted> _logger;

        public PostPayoutToLedgerOnPayoutCompleted(ILedgerRepository ledger, IServiceCatalogUnitOfWork unitOfWork, ILogger<PostPayoutToLedgerOnPayoutCompleted> logger)
        {
            _ledger = ledger;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleAsync(PayoutCompletedEvent e, CancellationToken cancellationToken)
        {
            var eventId = LedgerEventKeys.Payout(e.PayoutId.Value);
            // Gross = provider-payable cleared; commission recognized as revenue; net = paid out. Fall back to a
            // straight net transfer if the event predates gross/commission.
            var gross = e.GrossAmount ?? e.Amount;
            var commission = e.CommissionAmount;
            var tx = LedgerTransaction.ForPayout(eventId, e.ProviderId.Value, gross, commission);
            if (await _ledger.AppendAsync(tx, cancellationToken))
            {
                await _unitOfWork.CommitAsync(cancellationToken);
                _logger.LogInformation("Ledger payout posted for payout {PayoutId}", e.PayoutId.Value);
            }
        }
    }
}

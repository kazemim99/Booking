// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payout/ExecutePayout/ExecutePayoutCommandHandler.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.Core.Application.Abstractions;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Infrastructure.External.Payment;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Payout.ExecutePayout
{
    /// <summary>
    /// Handler for executing pending payouts via payment gateway
    /// </summary>
    public sealed class ExecutePayoutCommandHandler : ICommandHandler<ExecutePayoutCommand, ExecutePayoutResult>
    {
        private readonly IPayoutWriteRepository _payoutRepository;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly INotificationRaiser _notifications;
        private readonly Domain.Repositories.IProviderReadRepository _providers;
        private readonly ILogger<ExecutePayoutCommandHandler> _logger;

        public ExecutePayoutCommandHandler(
            IPayoutWriteRepository payoutRepository,
            IPaymentGateway paymentGateway,
            IServiceCatalogUnitOfWork unitOfWork,
            INotificationRaiser notifications,
            Domain.Repositories.IProviderReadRepository providers,
            ILogger<ExecutePayoutCommandHandler> logger)
        {
            _payoutRepository = payoutRepository ?? throw new ArgumentNullException(nameof(payoutRepository));
            _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _notifications = notifications;
            _providers = providers;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ExecutePayoutResult> Handle(ExecutePayoutCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Executing payout {PayoutId}", request.PayoutId);

            // Load payout
            var payoutId = PayoutId.From(request.PayoutId);
            var payout = await _payoutRepository.GetByIdAsync(payoutId, cancellationToken);

            // Answers to the caller, not server faults: InvalidOperationException is unmapped by
            // ExceptionHandlingMiddleware, so executing an unknown payout answered 500, not 404.
            if (payout == null)
            {
                throw new NotFoundException("Payout", request.PayoutId);
            }

            // Validate payout status
            if (payout.Status != PayoutStatus.Pending)
            {
                throw new DomainValidationException(
                    nameof(request.PayoutId),
                    $"Payout {request.PayoutId} cannot be executed. Current status: {payout.Status}");
            }

            // Validate net amount is positive
            if (payout.NetAmount.Amount <= 0)
            {
                throw new DomainValidationException(
                    nameof(request.PayoutId),
                    $"Payout {request.PayoutId} has invalid net amount: {payout.NetAmount.Amount}");
            }

            _logger.LogInformation("Processing payout {PayoutId} for provider {ProviderId}, amount: {Amount} {Currency}",
                request.PayoutId, payout.ProviderId.Value, payout.NetAmount.Amount, payout.NetAmount.Currency);

            // Create payout request for gateway
            var payoutRequest = new PayoutRequest
            {
                Amount = payout.NetAmount.Amount,
                Currency = payout.NetAmount.Currency,
                ConnectedAccountId = request.ConnectedAccountId,
                Description = $"Payout for period {payout.PeriodStart:yyyy-MM-dd} to {payout.PeriodEnd:yyyy-MM-dd}",
                Metadata = new Dictionary<string, object>
                {
                    ["PayoutId"] = payout.Id.Value.ToString(),
                    ["ProviderId"] = payout.ProviderId.Value.ToString(),
                    ["PeriodStart"] = payout.PeriodStart.ToString("O"),
                    ["PeriodEnd"] = payout.PeriodEnd.ToString("O")
                }
            };

            // Execute payout through gateway
            var result = await _paymentGateway.CreatePayoutAsync(payoutRequest, cancellationToken);

            if (result.IsSuccessful)
            {
                // Mark as processing/paid in domain
                payout.MarkAsProcessing(result.PayoutId!, request.ConnectedAccountId);

                // If Stripe says it's already paid (instant payout), mark as paid
                if (result.Status == "paid")
                {
                    payout.MarkAsPaid();
                }

                _logger.LogInformation("Payout {PayoutId} executed successfully. External ID: {ExternalId}, Status: {Status}",
                    payout.Id.Value, result.PayoutId, result.Status);
            }
            else
            {
                // Mark payout as failed
                payout.MarkAsFailed(result.ErrorMessage ?? "Unknown error");

                _logger.LogWarning("Payout {PayoutId} execution failed: {Error}",
                    payout.Id.Value, result.ErrorMessage);
            }

            // Persist our own single unit rather than relying on TransactionBehavior.
            //
            // ExecutePayoutCommand is INonTransactionalCommand (ADR-006) because CreatePayoutAsync above
            // moves real money: inside the ambient retried transaction, a transient DB fault would re-run
            // this handler and pay the provider twice. Opting out means nothing else will save for us, so
            // the commit has to happen here — one SaveChanges is itself a retry-safe unit.
            //
            // The commit deliberately follows the gateway call: if it fails after the payout succeeded,
            // the record is recoverable from the gateway's truth (the same reconciliation posture the
            // payment path takes), whereas committing first could mark a payout Completed that never left.
            await _payoutRepository.UpdateAsync(payout, cancellationToken);

            // The salon is entitled to know its money arrived. Addressed to the OWNER's user id, never
            // to the provider id: the inbox, preferences and device registry are all keyed by user, so a
            // provider id would address nobody. Recorded before the commit below so the same call writes
            // the payout and the notice of it.
            var paidProvider = await _providers.GetByIdAsync(payout.ProviderId, cancellationToken);
            if (paidProvider is not null)
            {
                await _notifications.RaiseAsync(
                    Domain.Enums.NotificationEventCode.PayoutCompleted,
                    paidProvider.OwnerId.Value,
                    dedupKey: payout.Id.Value,
                    parameters: new Dictionary<string, string>
                    {
                        [NotificationParameter.BusinessName] = paidProvider.Profile.BusinessName,
                        [NotificationParameter.Amount] = payout.NetAmount.Amount.ToString("N0"),
                    },
                    subjectType: "Payout",
                    subjectId: payout.Id.Value,
                    cancellationToken: cancellationToken);
            }
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            return new ExecutePayoutResult(
                payout.Id.Value,
                payout.ProviderId.Value,
                payout.GrossAmount.Amount,
                payout.CommissionAmount.Amount,
                payout.NetAmount.Amount,
                payout.NetAmount.Currency,
                payout.PeriodStart,
                payout.PeriodEnd,
                payout.PaymentIds.Count,
                payout.Status.ToString(),
                payout.ExternalPayoutId ?? string.Empty,
                result.ArrivalDate,
                result.IsSuccessful,
                result.ErrorMessage,
                payout.CreatedAt);
        }
    }
}

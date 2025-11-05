// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payout/ExecutePayout/ExecutePayoutCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions;
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
        private readonly ILogger<ExecutePayoutCommandHandler> _logger;

        public ExecutePayoutCommandHandler(
            IPayoutWriteRepository payoutRepository,
            IPaymentGateway paymentGateway,
            ILogger<ExecutePayoutCommandHandler> logger)
        {
            _payoutRepository = payoutRepository ?? throw new ArgumentNullException(nameof(payoutRepository));
            _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ExecutePayoutResult> Handle(ExecutePayoutCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Executing payout {PayoutId}", request.PayoutId);

            // Load payout
            var payoutId = PayoutId.From(request.PayoutId);
            var payout = await _payoutRepository.GetByIdAsync(payoutId, cancellationToken);

            if (payout == null)
            {
                throw new InvalidOperationException($"Payout {request.PayoutId} not found");
            }

            // Validate payout status
            if (payout.Status != PayoutStatus.Pending)
            {
                throw new InvalidOperationException($"Payout {request.PayoutId} cannot be executed. Current status: {payout.Status}");
            }

            // Validate net amount is positive
            if (payout.NetAmount.Amount <= 0)
            {
                throw new InvalidOperationException($"Payout {request.PayoutId} has invalid net amount: {payout.NetAmount.Amount}");
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

            // Update in repository (TransactionBehaviour will save)
            await _payoutRepository.UpdateAsync(payout, cancellationToken);

            return new ExecutePayoutResult(
                payout.Id.Value,
                payout.ProviderId.Value,
                payout.NetAmount.Amount,
                payout.NetAmount.Currency,
                payout.Status.ToString(),
                payout.ExternalPayoutId ?? string.Empty,
                result.ArrivalDate);
        }
    }
}

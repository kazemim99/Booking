// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payout/CreatePayout/CreatePayoutCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Payout.CreatePayout
{
    /// <summary>
    /// Handler for creating provider payouts. The payout amount is derived <b>from the ledger</b> — the single
    /// source of truth for provider balances — not by re-summing Payments. The ledger's ProviderPayable balance
    /// already nets charges against refunds and prior payouts, so refunds automatically reduce the payout and a
    /// provider whose refunds exceed unpaid charges is <b>blocked from further payouts</b> (balance ≤ 0).
    /// </summary>
    public sealed class CreatePayoutCommandHandler : ICommandHandler<CreatePayoutCommand, CreatePayoutResult>
    {
        private readonly ILedgerRepository _ledger;
        private readonly IPayoutWriteRepository _payoutRepository;
        private readonly ILogger<CreatePayoutCommandHandler> _logger;

        public CreatePayoutCommandHandler(
            ILedgerRepository ledger,
            IPayoutWriteRepository payoutRepository,
            ILogger<CreatePayoutCommandHandler> logger)
        {
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _payoutRepository = payoutRepository ?? throw new ArgumentNullException(nameof(payoutRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CreatePayoutResult> Handle(CreatePayoutCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating ledger-derived payout for provider {ProviderId}, period: {Start} to {End}",
                request.ProviderId, request.PeriodStart, request.PeriodEnd);

            var providerId = ProviderId.From(request.ProviderId);

            // Amount owed comes from the ledger (charges − refunds − prior payouts), never by re-summing Payments.
            var owed = await _ledger.GetProviderPayableBalanceAsync(providerId.Value, cancellationToken);
            if (owed <= 0m)
            {
                throw new InvalidOperationException(
                    $"Provider {request.ProviderId} has no payable ledger balance ({owed:0.00}); payout is blocked. " +
                    "Refunds may have offset unpaid charges.");
            }

            // Currency: use the provider's ledger currency (fall back to request/USD). Single-currency per provider today.
            var currency = "USD";
            var grossAmount = Money.Create(owed, currency);

            _logger.LogInformation("Ledger payable balance for provider {ProviderId}: {Amount} {Currency}",
                request.ProviderId, grossAmount.Amount, currency);

            // Calculate commission on the ledger-derived gross.
            var commissionRate = CommissionRate.CreatePercentage(request.CommissionPercentage ?? 15m);
            var commissionAmount = commissionRate.CalculateCommission(grossAmount);
            var netAmount = commissionRate.CalculateNetAmount(grossAmount);

            _logger.LogInformation("Commission: {Commission} {Currency}, Net: {Net} {Currency}",
                commissionAmount.Amount, currency, netAmount.Amount, currency);

            // Payment ids covered (audit trail) — derived from the ledger's charge entries for this provider.
            var chargePaymentIds = await _ledger.GetProviderChargePaymentIdsAsync(providerId.Value, cancellationToken);
            var paymentIds = chargePaymentIds.Select(PaymentId.From).ToList();

            // Create payout aggregate
            var payout = Domain.Aggregates.PayoutAggregate.Payout.Create(
                providerId,
                grossAmount,
                commissionAmount,
                request.PeriodStart,
                request.PeriodEnd,
                paymentIds,
                request.Notes);

            // Schedule if requested
            if (request.ScheduledAt.HasValue)
            {
                payout.Schedule(request.ScheduledAt.Value);
            }

            // Add to repository (TransactionBehaviour will save)
            await _payoutRepository.AddAsync(payout, cancellationToken);

            _logger.LogInformation("Payout {PayoutId} created successfully for provider {ProviderId}, net amount: {NetAmount} {Currency}",
                payout.Id.Value, request.ProviderId, netAmount.Amount, currency);

            return new CreatePayoutResult(
                payout.Id.Value,
                request.ProviderId,
                grossAmount.Amount,
                commissionAmount.Amount,
                netAmount.Amount,
                currency,
                request.PeriodStart,
                request.PeriodEnd,
                paymentIds.Count,
                payout.Status.ToString(),
                payout.CreatedAt);
        }
    }
}

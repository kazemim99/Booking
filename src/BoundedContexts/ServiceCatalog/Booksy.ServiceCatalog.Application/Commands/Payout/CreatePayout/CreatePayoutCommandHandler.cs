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
    /// Handler for creating provider payouts from completed payments
    /// </summary>
    public sealed class CreatePayoutCommandHandler : ICommandHandler<CreatePayoutCommand, CreatePayoutResult>
    {
        private readonly IPaymentReadRepository _paymentRepository;
        private readonly IPayoutWriteRepository _payoutRepository;
        private readonly ILogger<CreatePayoutCommandHandler> _logger;

        public CreatePayoutCommandHandler(
            IPaymentReadRepository paymentRepository,
            IPayoutWriteRepository payoutRepository,
            ILogger<CreatePayoutCommandHandler> logger)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _payoutRepository = payoutRepository ?? throw new ArgumentNullException(nameof(payoutRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CreatePayoutResult> Handle(CreatePayoutCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating payout for provider {ProviderId}, period: {Start} to {End}",
                request.ProviderId, request.PeriodStart, request.PeriodEnd);

            var providerId = ProviderId.From(request.ProviderId);

            // Get all completed payments for provider in the period
            var payments = await _paymentRepository.GetProviderPaymentsInRangeAsync(
                providerId,
                request.PeriodStart,
                request.PeriodEnd,
                PaymentStatus.Paid,
                cancellationToken);

            if (!payments.Any())
            {
                throw new InvalidOperationException(
                    $"No completed payments found for provider {request.ProviderId} in the specified period");
            }

            _logger.LogInformation("Found {Count} completed payments for payout", payments.Count);

            // Calculate gross amount (sum of all payment amounts)
            var currency = payments.First().Amount.Currency;
            var grossAmount = Money.Zero(currency);

            foreach (var payment in payments)
            {
                if (payment.Amount.Currency != currency)
                {
                    throw new InvalidOperationException(
                        $"Payment {payment.Id} has different currency ({payment.Amount.Currency}). All payments must have the same currency.");
                }

                grossAmount = grossAmount.Add(payment.Amount);
            }

            _logger.LogInformation("Gross amount calculated: {Amount} {Currency}",
                grossAmount.Amount, grossAmount.Currency);

            // Calculate commission
            var commissionRate = CommissionRate.CreatePercentage(request.CommissionPercentage ?? 15m);
            var commissionAmount = commissionRate.CalculateCommission(grossAmount);
            var netAmount = commissionRate.CalculateNetAmount(grossAmount);

            _logger.LogInformation("Commission: {Commission} {Currency}, Net: {Net} {Currency}",
                commissionAmount.Amount, currency, netAmount.Amount, currency);

            // Collect payment IDs
            var paymentIds = payments.Select(p => p.Id).ToList();

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
                payments.Count,
                payout.Status.ToString(),
                payout.CreatedAt);
        }
    }
}

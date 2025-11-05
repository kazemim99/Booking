// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payment/GetProviderEarnings/GetProviderEarningsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetProviderEarnings
{
    public sealed class GetProviderEarningsQueryHandler : IQueryHandler<GetProviderEarningsQuery, ProviderEarningsViewModel>
    {
        private readonly IPaymentReadRepository _paymentRepository;

        public GetProviderEarningsQueryHandler(IPaymentReadRepository paymentRepository)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        }

        public async Task<ProviderEarningsViewModel> Handle(GetProviderEarningsQuery request, CancellationToken cancellationToken)
        {
            var providerId = ProviderId.From(request.ProviderId);

            // Get all provider payments in the date range
            var payments = await _paymentRepository.GetProviderPaymentsInRangeAsync(
                providerId,
                request.StartDate,
                request.EndDate,
                null, // Get all statuses
                cancellationToken);

            if (!payments.Any())
            {
                return new ProviderEarningsViewModel(
                    request.ProviderId,
                    request.StartDate,
                    request.EndDate,
                    0,
                    0,
                    0,
                    "USD",
                    0,
                    0,
                    0,
                    0,
                    new List<EarningsByDateDto>());
            }

            var currency = payments.First().Amount.Currency;
            var commissionRate = CommissionRate.CreatePercentage(request.CommissionPercentage ?? 15m);

            // Calculate totals
            var paidPayments = payments.Where(p => p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.PartiallyRefunded).ToList();
            var refundedPayments = payments.Where(p => p.Status == PaymentStatus.Refunded || p.Status == PaymentStatus.PartiallyRefunded).ToList();

            var grossEarnings = Money.Zero(currency);
            var totalRefunded = Money.Zero(currency);

            foreach (var payment in paidPayments)
            {
                grossEarnings = grossEarnings.Add(payment.PaidAmount);
                totalRefunded = totalRefunded.Add(payment.RefundedAmount);
            }

            // Calculate commission and net
            var commissionAmount = commissionRate.CalculateCommission(grossEarnings);
            var netEarnings = grossEarnings.Subtract(commissionAmount).Subtract(totalRefunded);

            // Group by date
            var earningsByDate = paidPayments
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g =>
                {
                    var dailyGross = Money.Zero(currency);
                    foreach (var payment in g)
                    {
                        dailyGross = dailyGross.Add(payment.PaidAmount);
                    }

                    var dailyCommission = commissionRate.CalculateCommission(dailyGross);
                    var dailyNet = dailyGross.Subtract(dailyCommission);

                    return new EarningsByDateDto(
                        g.Key,
                        dailyGross.Amount,
                        dailyCommission.Amount,
                        dailyNet.Amount,
                        g.Count());
                })
                .OrderBy(e => e.Date)
                .ToList();

            return new ProviderEarningsViewModel(
                request.ProviderId,
                request.StartDate,
                request.EndDate,
                grossEarnings.Amount,
                commissionAmount.Amount,
                netEarnings.Amount,
                currency,
                payments.Count,
                paidPayments.Count,
                refundedPayments.Count,
                totalRefunded.Amount,
                earningsByDate);
        }
    }
}

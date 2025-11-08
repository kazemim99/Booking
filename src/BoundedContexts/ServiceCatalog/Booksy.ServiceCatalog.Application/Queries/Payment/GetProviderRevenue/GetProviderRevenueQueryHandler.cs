// ========================================
// GetProviderRevenueQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetProviderRevenue
{
    public sealed class GetProviderRevenueQueryHandler
        : IQueryHandler<GetProviderRevenueQuery, RevenueStatisticsViewModel>
    {
        private readonly IPaymentReadRepository _paymentRepository;

        public GetProviderRevenueQueryHandler(IPaymentReadRepository paymentRepository)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        }

        public async Task<RevenueStatisticsViewModel> Handle(
            GetProviderRevenueQuery request,
            CancellationToken cancellationToken)
        {
            var providerId = ProviderId.From(request.ProviderId);

            var (totalRevenue, totalRefunds, successfulPayments, totalPayments) =
                await _paymentRepository.GetProviderRevenueStatsAsync(
                    providerId,
                    request.StartDate,
                    request.EndDate,
                    cancellationToken);

            var netRevenue = totalRevenue - totalRefunds;
            var successRate = totalPayments > 0 ? (decimal)successfulPayments / totalPayments * 100 : 0;

            return new RevenueStatisticsViewModel(
                request.ProviderId,
                request.StartDate,
                request.EndDate,
                totalRevenue,
                totalRefunds,
                netRevenue,
                successfulPayments,
                totalPayments,
                successRate,
                "IRR"); // Default currency for ZarinPal
        }
    }
}

// ========================================
// GetProviderRevenueQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Payment.GetProviderRevenue
{
    public sealed record GetProviderRevenueQuery(
        Guid ProviderId,
        DateTime StartDate,
        DateTime EndDate) : IQuery<RevenueStatisticsViewModel>;
}

// ========================================
// GetProviderRevenueQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetProviderRevenue
{
    public sealed record GetProviderRevenueQuery(
        Guid ProviderId,
        DateTime StartDate,
        DateTime EndDate) : IQuery<RevenueStatisticsViewModel>;
}

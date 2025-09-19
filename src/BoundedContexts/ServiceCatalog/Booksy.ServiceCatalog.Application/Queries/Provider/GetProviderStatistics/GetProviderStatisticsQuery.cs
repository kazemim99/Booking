// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderStatistics/GetProviderStatisticsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderStatistics
{
    public sealed record GetProviderStatisticsQuery(
        Guid ProviderId,
        DateTime? FromDate = null,
        DateTime? ToDate = null) : IQuery<ProviderStatisticsViewModel>;
}
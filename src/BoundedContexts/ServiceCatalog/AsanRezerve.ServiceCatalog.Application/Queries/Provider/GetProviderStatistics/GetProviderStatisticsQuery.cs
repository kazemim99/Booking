// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Provider/GetProviderStatistics/GetProviderStatisticsQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetProviderStatistics
{
    public sealed record GetProviderStatisticsQuery(
        Guid ProviderId,
        DateTime? FromDate = null,
        DateTime? ToDate = null) : IQuery<ProviderStatisticsViewModel>;
}
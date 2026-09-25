// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Service/GetServiceStatistics/GetServiceStatisticsQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Service.GetServiceStatistics
{
    public sealed record GetServiceStatisticsQuery(
        Guid ServiceId,
        DateTime? StartDate = null,
        DateTime? EndDate = null) : IQuery<ServiceStatisticsViewModel>;
}


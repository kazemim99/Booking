// ========================================
// Booksy.ServiceCatalog.Application/Queries/Service/GetServiceStatistics/GetServiceStatisticsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Service.GetServiceStatistics
{
    public sealed record GetServiceStatisticsQuery(
        Guid ServiceId,
        DateTime? StartDate = null,
        DateTime? EndDate = null) : IQuery<ServiceStatisticsViewModel>;
}


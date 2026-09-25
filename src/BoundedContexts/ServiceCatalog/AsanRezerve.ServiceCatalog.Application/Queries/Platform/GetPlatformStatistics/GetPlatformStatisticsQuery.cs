// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Platform/GetPlatformStatistics/GetPlatformStatisticsQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Platform.GetPlatformStatistics
{
    /// <summary>
    /// Query to get platform-wide statistics for landing page
    /// </summary>
    public sealed record GetPlatformStatisticsQuery : IQuery<PlatformStatisticsViewModel>;
}

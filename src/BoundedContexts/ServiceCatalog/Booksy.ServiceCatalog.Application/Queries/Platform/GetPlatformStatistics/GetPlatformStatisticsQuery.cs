// ========================================
// Booksy.ServiceCatalog.Application/Queries/Platform/GetPlatformStatistics/GetPlatformStatisticsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Platform.GetPlatformStatistics
{
    /// <summary>
    /// Query to get platform-wide statistics for landing page
    /// </summary>
    public sealed record GetPlatformStatisticsQuery : IQuery<PlatformStatisticsViewModel>;
}

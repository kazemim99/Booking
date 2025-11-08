// ========================================
// Booksy.ServiceCatalog.Application/Queries/Reports/GetProviderPerformance/GetProviderPerformanceQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Reports.GetProviderPerformance
{
    public sealed record GetProviderPerformanceQuery(
        Guid ProviderId,
        DateTime? StartDate = null,
        DateTime? EndDate = null) : IQuery<ProviderPerformanceDto>;
}

// ========================================
// Booksy.ServiceCatalog.Application/Queries/Reports/GetRevenueReport/GetRevenueReportQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Reports.GetRevenueReport
{
    public sealed record GetRevenueReportQuery(
        Guid? ProviderId = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null) : IQuery<RevenueReportDto>;
}

// ========================================
// Booksy.ServiceCatalog.Application/Queries/Reports/GetBookingStatistics/GetBookingStatisticsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Reports.GetBookingStatistics
{
    public sealed record GetBookingStatisticsQuery(
        Guid? ProviderId = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null) : IQuery<BookingStatisticsDto>;
}

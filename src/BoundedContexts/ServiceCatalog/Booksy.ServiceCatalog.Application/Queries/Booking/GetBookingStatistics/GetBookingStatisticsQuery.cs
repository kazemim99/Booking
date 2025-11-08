// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetBookingStatistics/GetBookingStatisticsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetBookingStatistics
{
    public sealed record GetBookingStatisticsQuery(
        Guid ProviderId,
        DateTime? StartDate = null,
        DateTime? EndDate = null) : IQuery<BookingStatisticsDto>;
}

// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Booking/GetBookingStatistics/GetBookingStatisticsQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Booking.GetBookingStatistics
{
    public sealed record GetBookingStatisticsQuery(
        Guid ProviderId,
        DateTime? StartDate = null,
        DateTime? EndDate = null) : IQuery<BookingStatisticsDto>;
}

// ========================================
// Booksy.ServiceCatalog.Application/Queries/Reports/GetBookingStatistics/BookingStatisticsDto.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Reports.GetBookingStatistics
{
    public sealed record BookingStatisticsDto(
        int Total,
        int Requested,
        int Confirmed,
        int Completed,
        int Cancelled,
        int Rescheduled,
        int NoShows,
        decimal CompletionRate,
        decimal CancellationRate,
        decimal NoShowRate);
}

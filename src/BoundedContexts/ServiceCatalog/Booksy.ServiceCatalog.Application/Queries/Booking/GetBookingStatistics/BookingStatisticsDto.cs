// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetBookingStatistics/BookingStatisticsDto.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetBookingStatistics
{
    public sealed record BookingStatisticsDto(
        int TotalBookings,
        int RequestedBookings,
        int ConfirmedBookings,
        int CompletedBookings,
        int CancelledBookings,
        int NoShowBookings,
        int RescheduledBookings,
        decimal TotalRevenue,
        decimal CompletedRevenue,
        decimal PendingRevenue,
        decimal RefundedAmount,
        string Currency,
        double CompletionRate,
        double NoShowRate,
        double CancellationRate,
        DateTime? StartDate,
        DateTime? EndDate);
}

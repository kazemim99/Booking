// ========================================
// Booksy.ServiceCatalog.Application/Queries/Reports/GetProviderPerformance/ProviderPerformanceDto.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Reports.GetProviderPerformance
{
    public sealed record ProviderPerformanceDto(
        Guid ProviderId,
        int TotalBookings,
        int CompletedBookings,
        int CancelledBookings,
        int NoShowBookings,
        decimal TotalRevenue,
        string Currency,
        decimal AverageBookingDurationMinutes,
        decimal CompletionRate,
        IReadOnlyList<TopServiceDto> TopServices);

    public sealed record TopServiceDto(
        Guid ServiceId,
        int BookingCount);
}

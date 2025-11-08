// ========================================
// Booksy.ServiceCatalog.Application/Queries/Reports/GetRevenueReport/RevenueReportDto.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Reports.GetRevenueReport
{
    public sealed record RevenueReportDto(
        decimal TotalRevenue,
        string Currency,
        decimal AverageBookingValue,
        int TotalBookings,
        int PaidBookings,
        IReadOnlyList<DailyRevenueDto> RevenueByDate);

    public sealed record DailyRevenueDto(
        DateTime Date,
        decimal Amount,
        string Currency);
}

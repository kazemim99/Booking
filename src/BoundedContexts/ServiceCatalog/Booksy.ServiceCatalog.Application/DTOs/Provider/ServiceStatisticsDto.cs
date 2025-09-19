// ========================================
// Booksy.ServiceCatalog.Application/DTOs/Service/ServiceStatisticsDto.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.DTOs.Service
{
    public sealed class ServiceStatisticsDto
    {
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public decimal AverageRating { get; set; }
        public decimal Revenue { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime? LastBookingDate { get; set; }
        public int ActiveOptionsCount { get; set; }
        public int PriceTiersCount { get; set; }
        public int QualifiedStaffCount { get; set; }
        public Dictionary<string, int> BookingsByMonth { get; set; } = new();
        public Dictionary<string, decimal> RevenueByMonth { get; set; } = new();
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageBookingValue { get; set; }
        public string BookingTrend { get; set; }
    }
}
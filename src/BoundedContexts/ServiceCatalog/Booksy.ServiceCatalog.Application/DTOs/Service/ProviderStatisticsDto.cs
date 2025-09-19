// ========================================
// Booksy.ServiceCatalog.Application/DTOs/Provider/ProviderStatisticsDto.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.DTOs.Provider
{
    public sealed class ProviderStatisticsDto
    {
        public Guid ProviderId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public int TotalServices { get; set; }
        public int ActiveServices { get; set; }
        public int TotalStaff { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal AverageRating { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
        public Dictionary<string, int> BookingsByMonth { get; set; } = new();
        public Dictionary<string, decimal> RevenueByMonth { get; set; } = new();
        public Dictionary<string, int> ServicesByCategory { get; set; } = new();
        public int ActiveBookingsThisMonth { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public double GrowthRate { get; set; }
    }
}
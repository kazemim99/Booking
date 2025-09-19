// ========================================
// Booksy.ServiceCatalog.Application/Queries/Service/GetServiceStatistics/GetServiceStatisticsQuery.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Service.GetServiceStatistics
{
    public sealed class ServiceStatisticsViewModel
    {
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public ServiceStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // Booking Statistics
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int NoShowBookings { get; set; }
        public double BookingCompletionRate { get; set; }

        // Revenue Statistics
        public decimal TotalRevenue { get; set; }
        public decimal AverageBookingValue { get; set; }

        // Metadata
        public string StatisticsPeriod { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }
}


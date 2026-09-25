// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Service/GetServiceStatistics/GetServiceStatisticsQuery.cs
// ========================================
// ========================================
// AsanRezerve.ServiceCatalog.Application/Services/Interfaces/IImageProcessingService.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Services.Interfaces
{
    // Supporting classes for repository statistics
    public sealed class BookingStatistics
    {
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int NoShowBookings { get; set; }
    }
}
// ========================================
// Booksy.ServiceCatalog.Application/Queries/Service/GetServiceStatistics/GetServiceStatisticsQuery.cs
// ========================================
// ========================================
// Booksy.ServiceCatalog.Application/Services/Interfaces/IImageProcessingService.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Services.Interfaces
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
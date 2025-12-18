// ========================================
// Booksy.ServiceCatalog.Application/Queries/Platform/GetPlatformStatistics/PlatformStatisticsViewModel.cs
// ========================================

namespace Booksy.ServiceCatalog.Application.Queries.Platform.GetPlatformStatistics
{
    /// <summary>
    /// Platform-wide statistics for landing page display
    /// </summary>
    public sealed class PlatformStatisticsViewModel
    {
        /// <summary>
        /// Total number of active service providers on the platform
        /// </summary>
        public int TotalProviders { get; set; }

        /// <summary>
        /// Total number of registered customers (would come from Customer context)
        /// </summary>
        public int TotalCustomers { get; set; }

        /// <summary>
        /// Total number of completed bookings (would come from Booking context)
        /// </summary>
        public int TotalBookings { get; set; }

        /// <summary>
        /// Average rating across all providers (would come from Reviews context)
        /// </summary>
        public decimal AverageRating { get; set; }

        /// <summary>
        /// Total number of services offered across all providers
        /// </summary>
        public int TotalServices { get; set; }

        /// <summary>
        /// Number of cities with active providers
        /// </summary>
        public int CitiesWithProviders { get; set; }

        /// <summary>
        /// Most popular service categories with provider counts
        /// </summary>
        public Dictionary<string, int> PopularCategories { get; set; } = new();

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }
}

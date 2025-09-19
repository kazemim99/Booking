// ========================================
// Booksy.ServiceCatalog.Infrastructure/ReadModels/ServiceReadModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Infrastructure.ReadModels
{
    public sealed class ServiceReadModel
    {
        public Guid Id { get; set; }
        public Guid ProviderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryDescription { get; set; }
        public string? CategoryIconUrl { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Pricing
        public decimal BasePriceAmount { get; set; }
        public string BasePriceCurrency { get; set; } = string.Empty;
        public bool RequiresDeposit { get; set; }
        public decimal DepositPercentage { get; set; }

        // Duration
        public int DurationMinutes { get; set; }
        public int? PreparationTimeMinutes { get; set; }
        public int? BufferTimeMinutes { get; set; }

        // Availability
        public bool AllowOnlineBooking { get; set; }
        public bool AvailableAtLocation { get; set; }
        public bool AvailableAsMobile { get; set; }

        // Booking Rules
        public int MaxAdvanceBookingDays { get; set; }
        public int MinAdvanceBookingHours { get; set; }
        public int MaxConcurrentBookings { get; set; }

        // Metadata
        public string? ImageUrl { get; set; }
        public List<string> Tags { get; set; } = new();

        // Provider Information (for denormalized queries)
        public string ProviderBusinessName { get; set; } = string.Empty;
        public string ProviderCity { get; set; } = string.Empty;
        public string ProviderType { get; set; } = string.Empty;
        public bool ProviderOffersMobileServices { get; set; }

        // Statistics
        public int OptionsCount { get; set; }
        public int PriceTiersCount { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }
}
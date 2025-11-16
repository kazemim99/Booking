// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderProfile/ProviderProfileViewModel.cs
// ========================================
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderProfile
{
    /// <summary>
    /// Comprehensive provider profile view model for customer-facing pages
    /// Aggregates all data needed for rich profile display
    /// </summary>
    public sealed class ProviderProfileViewModel
    {
        // Basic Info
        public Guid ProviderId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? ProfileImageUrl { get; set; }
        public ProviderType Type { get; set; }
        public ProviderStatus Status { get; set; }

        // Contact & Location
        public ContactInfo ContactInfo { get; set; } = null!;
        public AddressInfo Address { get; set; } = null!;

        // Business Settings
        public bool AllowOnlineBooking { get; set; }
        public bool OffersMobileServices { get; set; }
        public bool IsVerified { get; set; }
        public PriceRange PriceRange { get; set; }

        // Rating & Social Proof
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<ReviewSummaryViewModel> RecentReviews { get; set; } = new();

        // Services
        public int TotalServices { get; set; }
        public List<ServiceDetailViewModel> Services { get; set; } = new();

        // Staff
        public int TotalStaff { get; set; }
        public List<StaffProfileViewModel> Staff { get; set; } = new();

        // Gallery
        public List<GalleryImageViewModel> GalleryImages { get; set; } = new();

        // Business Hours
        public Dictionary<DayOfWeek, BusinessHoursDto?> BusinessHours { get; set; } = new();

        // Availability Summary
        public AvailabilitySummaryViewModel AvailabilitySummary { get; set; } = null!;

        // Statistics
        public ProviderStatsViewModel Statistics { get; set; } = null!;

        // Tags & Categories
        public List<string> Tags { get; set; } = new();
        public List<string> ServiceCategories { get; set; } = new();

        // Timestamps
        public DateTime RegisteredAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
        public int YearsInBusiness { get; set; }
    }

    public sealed class ReviewSummaryViewModel
    {
        public Guid ReviewId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ServiceName { get; set; }
    }

    public sealed class ServiceDetailViewModel
    {
        public Guid ServiceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public int DurationMinutes { get; set; }
        public bool IsPopular { get; set; }
    }

    public sealed class StaffProfileViewModel
    {
        public Guid StaffId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public decimal? AverageRating { get; set; }
    }

    public sealed class GalleryImageViewModel
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public bool IsPrimary { get; set; }
        public int DisplayOrder { get; set; }
    }

    public sealed class AvailabilitySummaryViewModel
    {
        public DateTime? NextAvailableSlot { get; set; }
        public int AvailableSlotsNext7Days { get; set; }
        public double AverageAvailabilityPercentage { get; set; }
    }

    public sealed class ProviderStatsViewModel
    {
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal ResponseRate { get; set; }
        public int RepeatCustomers { get; set; }
    }
}

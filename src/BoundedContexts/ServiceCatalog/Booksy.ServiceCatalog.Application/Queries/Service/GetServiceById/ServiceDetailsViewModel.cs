// ========================================
// Booksy.ServiceCatalog.Application/Queries/Service/GetServiceById/ServiceDetailsViewModel.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Queries.Service.GetServiceById
{
    public sealed class ServiceDetailsViewModel
    {
        public Guid Id { get; set; }
        public Guid ProviderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public ServiceType Type { get; set; }
        public decimal BasePrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int? PreparationTime { get; set; }
        public int? BufferTime { get; set; }
        public int TotalDuration { get; set; }
        public ServiceStatus Status { get; set; }
        public bool RequiresDeposit { get; set; }
        public decimal DepositPercentage { get; set; }
        public decimal DepositAmount { get; set; }
        public bool AllowOnlineBooking { get; set; }
        public bool AvailableAtLocation { get; set; }
        public bool AvailableAsMobile { get; set; }
        public int MaxAdvanceBookingDays { get; set; }
        public int MinAdvanceBookingHours { get; set; }
        public int MaxConcurrentBookings { get; set; }
        public string? ImageUrl { get; set; }
        public List<string> Tags { get; set; } = new();
        public int QualifiedStaffCount { get; set; }
        public bool CanBeBooked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }

        // Navigation properties
        public ProviderSummaryViewModel? Provider { get; set; }
        public List<ServiceOptionViewModel> Options { get; set; } = new();
        public List<PriceTierViewModel> PriceTiers { get; set; } = new();
    }

    public sealed class PriceTierViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }

    public sealed class ProviderSummaryViewModel
    {
        public Guid Id { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ProviderType Type { get; set; }
        public string? LogoUrl { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public bool AllowOnlineBooking { get; set; }
        public bool OffersMobileServices { get; set; }
    }
}
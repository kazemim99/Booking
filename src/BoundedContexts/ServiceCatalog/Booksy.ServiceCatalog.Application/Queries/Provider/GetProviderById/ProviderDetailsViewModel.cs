// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderById/ProviderDetailsViewModel.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById
{
    public sealed class ProviderDetailsViewModel
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public ProviderStatus Status { get; set; }
        public ProviderType Type { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PrimaryPhone { get; set; } = string.Empty;
        public string? SecondaryPhone { get; set; }
        public AddressViewModel Address { get; set; } = new();
        public bool RequiresApproval { get; set; }
        public bool AllowOnlineBooking { get; set; }
        public bool OffersMobileServices { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, string> SocialMedia { get; set; } = new();
        public List<BusinessHoursViewModel> BusinessHours { get; set; } = new();
        public List<StaffViewModel> Staff { get; set; } = new();
        public List<ServiceSummaryViewModel> Services { get; set; } = new();
        public int ActiveServicesCount { get; set; }
    }
}
// ========================================
// Booksy.ServiceCatalog.Application/DTOs/Provider/ProviderSummaryDto.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.DTOs.Provider
{
    public sealed class ProviderSummaryDto
    {
        public Guid Id { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ProviderStatus Status { get; set; }
        public ServiceCategory PrimaryCategory { get; set; }
        public string? LogoUrl { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public bool AllowOnlineBooking { get; set; }
        public bool OffersMobileServices { get; set; }
        public List<string> Tags { get; set; } = new();
        public int ActiveServicesCount { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
    }
}
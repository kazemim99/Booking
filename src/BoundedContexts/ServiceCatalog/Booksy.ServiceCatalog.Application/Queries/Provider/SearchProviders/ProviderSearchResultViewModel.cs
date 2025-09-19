// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/SearchProviders/ProviderSearchResultViewModel.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.SearchProviders
{
    public sealed class ProviderSearchResultViewModel
    {
        public Guid Id { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ProviderType Type { get; set; }
        public string? LogoUrl { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PrimaryPhone { get; set; } = string.Empty;
        public string? Website { get; set; }
        public bool AllowOnlineBooking { get; set; }
        public bool OffersMobileServices { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime? LastActiveAt { get; set; }
    }
}
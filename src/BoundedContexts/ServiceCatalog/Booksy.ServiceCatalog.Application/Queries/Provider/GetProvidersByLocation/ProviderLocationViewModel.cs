using Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProvidersByLocation
{
    public sealed class ProviderLocationViewModel
    {
        public Guid Id { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ServiceCategory PrimaryCategory { get; set; }
        public string? LogoUrl { get; set; }
        public AddressViewModel Address { get; set; } = new();
        public string Email { get; set; } = string.Empty;
        public string PrimaryPhone { get; set; } = string.Empty;
        public string? Website { get; set; }
        public bool AllowOnlineBooking { get; set; }
        public bool OffersMobileServices { get; set; }
        public List<string> Tags { get; set; } = new();
        public double Distance { get; set; } // Distance in kilometers
    }
}
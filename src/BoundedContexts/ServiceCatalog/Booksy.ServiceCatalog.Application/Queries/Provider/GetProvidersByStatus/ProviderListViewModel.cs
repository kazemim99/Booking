
namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProvidersByStatus
{
    public sealed class ProviderListViewModel
    {
        public Guid Id { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ProviderStatus Status { get; set; }
        public ProviderType Type { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PrimaryPhone { get; set; } = string.Empty;
        public bool AllowOnlineBooking { get; set; }
        public bool OffersMobileServices { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
    }
}
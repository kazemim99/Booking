namespace Booksy.ServiceCatalog.Infrastructure.ReadModels
{
    public sealed class ProviderReadModel
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        // Contact Information
        public string Email { get; set; } = string.Empty;
        public string? PrimaryPhone { get; set; }
        public string? SecondaryPhone { get; set; }
        public string? Website { get; set; }

        // Address
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Settings
        public bool RequiresApproval { get; set; }
        public bool AllowOnlineBooking { get; set; }
        public bool OffersMobileServices { get; set; }

        // Timestamps
        public DateTime RegisteredAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime? LastActiveAt { get; set; }

        // Statistics
        public int ActiveServicesCount { get; set; }
        public int TotalStaffCount { get; set; }
        public decimal? AverageServicePrice { get; set; }
        public string? AverageServicePriceCurrency { get; set; }
    }
}

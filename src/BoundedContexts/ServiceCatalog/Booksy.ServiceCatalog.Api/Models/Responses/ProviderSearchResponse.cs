//===========================================
// Models/Responses/ProviderResponse.cs
//===========================================
namespace Booksy.ServiceCatalog.Api.Models.Responses
{
    public sealed class ProviderSearchResponse
    {
        public Guid Id { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public bool AllowOnlineBooking { get; set; }
        public bool OffersMobileServices { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int ServiceCount { get; set; }
        public int YearsInBusiness { get; set; }
        public bool IsVerified { get; set; }
        public List<string> Tags { get; set; } = new();
        public string? OperatingHours { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
    }
}

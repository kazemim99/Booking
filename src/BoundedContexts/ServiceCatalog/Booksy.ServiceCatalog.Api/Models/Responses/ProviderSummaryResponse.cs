namespace Booksy.ServiceCatalog.API.Models.Responses;

public class ProviderSummaryResponse
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool AllowOnlineBooking { get; set; }
    public bool OffersMobileServices { get; set; }
    public List<string>? Tags { get; set; }
    public dynamic IsVerified { get; internal set; }
    public dynamic TotalReviews { get; internal set; }
    public dynamic ServiceCount { get; internal set; }
    public dynamic RegisteredAt { get; internal set; }
    public dynamic AverageRating { get; internal set; }
}

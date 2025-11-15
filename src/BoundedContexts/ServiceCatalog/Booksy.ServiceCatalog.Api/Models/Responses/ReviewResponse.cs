namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Response model for a review
/// </summary>
public class ReviewResponse
{
    public Guid ReviewId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid BookingId { get; set; }
    public decimal Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsVerified { get; set; }
    public string? ProviderResponse { get; set; }
    public DateTime? ProviderResponseAt { get; set; }
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
    public decimal HelpfulnessRatio { get; set; }
    public bool IsConsideredHelpful { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AgeInDays { get; set; }
    public bool IsRecent { get; set; }
}

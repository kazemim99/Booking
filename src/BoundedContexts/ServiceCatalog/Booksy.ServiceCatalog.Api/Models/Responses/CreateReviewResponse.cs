namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Response model for creating a review
/// </summary>
public class CreateReviewResponse
{
    public Guid ReviewId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid BookingId { get; set; }
    public decimal Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}

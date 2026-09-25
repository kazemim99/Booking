namespace AsanRezerve.ServiceCatalog.Api.Models.Responses;

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

    /// <summary>"Pending" on creation: not public until an administrator approves it.</summary>
    public string ModerationStatus { get; set; } = string.Empty;

    public decimal? CleanlinessRating { get; set; }
    public decimal? SkillRating { get; set; }
    public decimal? PunctualityRating { get; set; }
    public decimal? ConductRating { get; set; }
}

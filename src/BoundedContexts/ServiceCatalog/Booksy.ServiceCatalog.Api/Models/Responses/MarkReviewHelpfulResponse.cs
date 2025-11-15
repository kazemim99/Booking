namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Response model for marking a review as helpful
/// </summary>
public class MarkReviewHelpfulResponse
{
    public Guid ReviewId { get; set; }
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
    public decimal HelpfulnessRatio { get; set; }
    public bool IsConsideredHelpful { get; set; }
}

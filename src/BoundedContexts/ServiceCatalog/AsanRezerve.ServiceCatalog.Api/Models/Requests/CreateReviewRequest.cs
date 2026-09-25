using System.ComponentModel.DataAnnotations;

namespace AsanRezerve.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for creating a review for a completed booking
/// </summary>
public class CreateReviewRequest
{
    /// <summary>
    /// Overall rating (1.0 to 5.0 in 0.5 increments). Optional: the current forms leave it out and send all four
    /// aspects, and the overall is their average to the nearest half star. Older app versions still send it, and it
    /// is kept as sent (openspec/changes/_inline/reviews-and-reschedule-round2 D1).
    /// </summary>
    [Range(1.0, 5.0, ErrorMessage = "Rating must be between 1.0 and 5.0")]
    public decimal? Rating { get; set; }

    /// <summary>
    /// Sign the public review with the author's name (default). False — «نامم نمایش داده نشود» — signs it «مشتری».
    /// </summary>
    public bool ShowName { get; set; } = true;

    /// <summary>
    /// Optional review comment (10-2000 characters)
    /// Supports Persian and English text
    /// </summary>
    [StringLength(2000, MinimumLength = 10,
        ErrorMessage = "Comment must be between 10 and 2000 characters")]
    public string? Comment { get; set; }

    // The four aspects. All four are required when Rating is left out; with a Rating (older apps) each may be left
    // out. Half-star increments are enforced by the domain, which names the offending field in its error.
    [Range(1.0, 5.0, ErrorMessage = "CleanlinessRating must be between 1.0 and 5.0")]
    public decimal? CleanlinessRating { get; set; }

    [Range(1.0, 5.0, ErrorMessage = "SkillRating must be between 1.0 and 5.0")]
    public decimal? SkillRating { get; set; }

    [Range(1.0, 5.0, ErrorMessage = "PunctualityRating must be between 1.0 and 5.0")]
    public decimal? PunctualityRating { get; set; }

    [Range(1.0, 5.0, ErrorMessage = "ConductRating must be between 1.0 and 5.0")]
    public decimal? ConductRating { get; set; }
}

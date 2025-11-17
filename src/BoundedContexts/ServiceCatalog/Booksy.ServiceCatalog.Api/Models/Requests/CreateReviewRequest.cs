using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for creating a review for a completed booking
/// </summary>
public class CreateReviewRequest
{
    /// <summary>
    /// Rating value (1.0 to 5.0 in 0.5 increments)
    /// </summary>
    [Required]
    [Range(1.0, 5.0, ErrorMessage = "Rating must be between 1.0 and 5.0")]
    public decimal Rating { get; set; }

    /// <summary>
    /// Optional review comment (10-2000 characters)
    /// Supports Persian and English text
    /// </summary>
    [StringLength(2000, MinimumLength = 10,
        ErrorMessage = "Comment must be between 10 and 2000 characters")]
    public string? Comment { get; set; }
}

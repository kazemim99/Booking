using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for marking a review as helpful or not helpful
/// </summary>
public class MarkReviewHelpfulRequest
{
    /// <summary>
    /// True if marking as helpful, false if marking as not helpful
    /// </summary>
    [Required]
    public bool IsHelpful { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for getting provider reviews with pagination and filtering
/// </summary>
public class GetProviderReviewsRequest
{
    /// <summary>
    /// Page number (1-based, default: 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size (1-100, default: 20)
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Filter by minimum rating (optional)
    /// </summary>
    [Range(1.0, 5.0, ErrorMessage = "Minimum rating must be between 1.0 and 5.0")]
    public decimal? MinRating { get; set; }

    /// <summary>
    /// Filter by maximum rating (optional)
    /// </summary>
    [Range(1.0, 5.0, ErrorMessage = "Maximum rating must be between 1.0 and 5.0")]
    public decimal? MaxRating { get; set; }

    /// <summary>
    /// Show only verified reviews (optional)
    /// </summary>
    public bool? VerifiedOnly { get; set; }

    /// <summary>
    /// Sort by field: "date", "rating", "helpful" (default: "date")
    /// </summary>
    public string SortBy { get; set; } = "date";

    /// <summary>
    /// Sort descending (default: true - newest/highest first)
    /// </summary>
    public bool SortDescending { get; set; } = true;
}

namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Response model for provider reviews with statistics and pagination
/// </summary>
public class ProviderReviewsResponse
{
    public Guid ProviderId { get; set; }
    public ReviewStatisticsResponse Statistics { get; set; } = new();
    public PaginatedReviewsResponse Reviews { get; set; } = new();
}

/// <summary>
/// Review statistics for a provider
/// </summary>
public class ReviewStatisticsResponse
{
    public int TotalReviews { get; set; }
    public int VerifiedReviews { get; set; }
    public decimal AverageRating { get; set; }
    public RatingDistributionResponse RatingDistribution { get; set; } = new();
    public int ReviewsWithComments { get; set; }
    public int ReviewsWithProviderResponse { get; set; }
    public DateTime? MostRecentReviewDate { get; set; }
    public DateTime? OldestReviewDate { get; set; }
}

/// <summary>
/// Distribution of ratings by star count
/// </summary>
public class RatingDistributionResponse
{
    public int FiveStarCount { get; set; }
    public int FourStarCount { get; set; }
    public int ThreeStarCount { get; set; }
    public int TwoStarCount { get; set; }
    public int OneStarCount { get; set; }
    public decimal FiveStarPercentage { get; set; }
    public decimal FourStarPercentage { get; set; }
    public decimal ThreeStarPercentage { get; set; }
    public decimal TwoStarPercentage { get; set; }
    public decimal OneStarPercentage { get; set; }
}

/// <summary>
/// Paginated reviews result
/// </summary>
public class PaginatedReviewsResponse
{
    public List<ReviewResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

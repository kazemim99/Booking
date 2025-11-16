using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Review.GetProviderReviews;

/// <summary>
/// Query to get paginated reviews for a provider
/// </summary>
public sealed record GetProviderReviewsQuery(
    Guid ProviderId,
    int PageNumber = 1,
    int PageSize = 20,
    decimal? MinRating = null,
    decimal? MaxRating = null,
    bool? VerifiedOnly = null,
    string SortBy = "date",
    bool SortDescending = true) : IQuery<GetProviderReviewsViewModel>;

/// <summary>
/// View model for provider reviews response
/// </summary>
public sealed record GetProviderReviewsViewModel(
    Guid ProviderId,
    ReviewStatisticsViewModel Statistics,
    PaginatedReviewsViewModel Reviews);

/// <summary>
/// Review statistics for the provider
/// </summary>
public sealed record ReviewStatisticsViewModel(
    int TotalReviews,
    int VerifiedReviews,
    decimal AverageRating,
    RatingDistributionViewModel RatingDistribution,
    int ReviewsWithComments,
    int ReviewsWithProviderResponse,
    DateTime? MostRecentReviewDate,
    DateTime? OldestReviewDate);

/// <summary>
/// Distribution of ratings by star count
/// </summary>
public sealed record RatingDistributionViewModel(
    int FiveStarCount,
    int FourStarCount,
    int ThreeStarCount,
    int TwoStarCount,
    int OneStarCount,
    decimal FiveStarPercentage,
    decimal FourStarPercentage,
    decimal ThreeStarPercentage,
    decimal TwoStarPercentage,
    decimal OneStarPercentage);

/// <summary>
/// Paginated reviews result
/// </summary>
public sealed record PaginatedReviewsViewModel(
    List<ReviewItemViewModel> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);

/// <summary>
/// Individual review item
/// </summary>
public sealed record ReviewItemViewModel(
    Guid ReviewId,
    Guid ProviderId,
    Guid CustomerId,
    string CustomerName,
    Guid BookingId,
    decimal Rating,
    string? Comment,
    bool IsVerified,
    string? ProviderResponse,
    DateTime? ProviderResponseAt,
    int HelpfulCount,
    int NotHelpfulCount,
    decimal HelpfulnessRatio,
    bool IsConsideredHelpful,
    DateTime CreatedAt,
    int AgeInDays,
    bool IsRecent);

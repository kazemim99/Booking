using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories;

/// <summary>
/// Read repository for querying Review aggregate
/// Supports pagination, filtering, and statistics
/// </summary>
public interface IReviewReadRepository : IReadRepository<Review, Guid>
{
    /// <summary>
    /// Get review by ID
    /// </summary>
    Task<Review?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get review by booking ID (one review per booking)
    /// </summary>
    Task<Review?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get reviews for a provider with pagination and filtering
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (max 100)</param>
    /// <param name="minRating">Filter by minimum rating (optional)</param>
    /// <param name="maxRating">Filter by maximum rating (optional)</param>
    /// <param name="verifiedOnly">Show only verified reviews (optional)</param>
    /// <param name="sortBy">Sort order: "date", "rating", "helpful" (default: "date")</param>
    /// <param name="sortDescending">Sort descending (default: true)</param>
    Task<PaginatedReviews> GetByProviderIdAsync(
        ProviderId providerId,
        int pageNumber = 1,
        int pageSize = 20,
        decimal? minRating = null,
        decimal? maxRating = null,
        bool? verifiedOnly = null,
        string sortBy = "date",
        bool sortDescending = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get reviews created by a customer
    /// </summary>
    Task<IReadOnlyList<Review>> GetByCustomerIdAsync(
        UserId customerId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get review statistics for a provider
    /// </summary>
    Task<ReviewStatistics> GetReviewStatisticsAsync(
        ProviderId providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a booking already has a review
    /// </summary>
    Task<bool> HasReviewAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent reviews across all providers (for homepage/feed)
    /// </summary>
    Task<IReadOnlyList<Review>> GetRecentReviewsAsync(
        int count = 10,
        bool verifiedOnly = true,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Paginated review results
/// </summary>
public record PaginatedReviews(
    IReadOnlyList<Review> Reviews,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

/// <summary>
/// Review statistics for a provider
/// </summary>
public record ReviewStatistics(
    int TotalReviews,
    int VerifiedReviews,
    decimal AverageRating,
    int FiveStarCount,
    int FourStarCount,
    int ThreeStarCount,
    int TwoStarCount,
    int OneStarCount,
    int ReviewsWithComments,
    int ReviewsWithProviderResponse,
    DateTime? MostRecentReviewDate,
    DateTime? OldestReviewDate);

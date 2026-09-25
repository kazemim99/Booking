using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Repositories;

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
        CancellationToken cancellationToken = default,
        bool publishedOnly = true);

    /// <summary>
    /// Which way this user voted on each of these reviews (true helpful, false not). Reviews they have not voted on
    /// are absent.
    /// </summary>
    /// <summary>
    /// How many of a provider's published reviews still wait on the business: no reply yet, or a reply an
    /// administrator refused. Counted over every review, so it does not depend on which page a client read.
    /// </summary>
    Task<int> CountAwaitingReplyAsync(ProviderId providerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, bool>> GetVotesByUserAsync(
        UserId userId,
        IReadOnlyCollection<Guid> reviewIds,
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
    /// Review statistics for a provider — over PUBLISHED reviews only. Pending, rejected and hidden reviews
    /// contribute to nothing here, including the verified count and the distribution.
    /// </summary>
    Task<ReviewStatistics> GetReviewStatisticsAsync(
        ProviderId providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a booking already has a review
    /// </summary>
    Task<bool> HasReviewAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The review written for each of these bookings, in whatever moderation state. Bookings without a review are
    /// absent. One query however many bookings — the customer's booking list asks for a page at a time.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, BookingReviewState>> GetStatesByBookingIdsAsync(
        IReadOnlyCollection<Guid> bookingIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The administrator's moderation queue. <see cref="ReviewModerationFilter.Pending"/> holds every review or
    /// provider reply awaiting a decision, oldest first; <see cref="ReviewModerationFilter.Hidden"/> is where
    /// hidden reviews are found, since they are no longer pending; <see cref="ReviewModerationFilter.Reported"/>
    /// holds published reviews someone has reported.
    /// </summary>
    Task<PaginatedReviews> GetModerationQueueAsync(
        ReviewModerationFilter filter,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The salon and service each review is about, for lists shown to the reviews' author. Batched: one query per
    /// table, however many reviews.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, ReviewContext>> GetReviewContextsAsync(
        IReadOnlyCollection<Review> reviews,
        CancellationToken cancellationToken = default);

    /// <summary>Every report filed against these reviews, oldest first.</summary>
    Task<IReadOnlyList<ReviewReport>> GetReportsAsync(
        IReadOnlyCollection<Guid> reviewIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent reviews across all providers (for homepage/feed)
    /// </summary>
    Task<IReadOnlyList<Review>> GetRecentReviewsAsync(
        int count = 10,
        bool verifiedOnly = true,
        CancellationToken cancellationToken = default);
}

/// <summary>The review a booking has: which one, and where moderation stands on it.</summary>
public sealed record BookingReviewState(Guid ReviewId, Enums.ReviewModerationStatus ModerationStatus);

/// <summary>What a review is about, by name.</summary>
public sealed record ReviewContext(string ProviderName, string? ProviderLogoUrl, string? ServiceName);

/// <summary>Which slice of the moderation queue to list.</summary>
public enum ReviewModerationFilter
{
    Pending,
    Hidden,
    Reported,
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
    DateTime? OldestReviewDate,
    DimensionRating Cleanliness,
    DimensionRating Skill,
    DimensionRating Punctuality,
    DimensionRating Conduct);

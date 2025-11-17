using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories;

/// <summary>
/// Read repository implementation for Review aggregate
/// Handles all read operations with pagination and filtering
/// </summary>
public sealed class ReviewReadRepository
    : EfReadRepositoryBase<Review, Guid, ServiceCatalogDbContext>,
      IReviewReadRepository
{
    private readonly ILogger<ReviewReadRepository> _logger;

    public ReviewReadRepository(
        ServiceCatalogDbContext context,
        ILogger<ReviewReadRepository> logger)
        : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Review?> GetByIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
    }

    public async Task<Review?> GetByBookingIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BookingId == bookingId, cancellationToken);
    }

    public async Task<PaginatedReviews> GetByProviderIdAsync(
        ProviderId providerId,
        int pageNumber = 1,
        int pageSize = 20,
        decimal? minRating = null,
        decimal? maxRating = null,
        bool? verifiedOnly = null,
        string sortBy = "date",
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = DbSet
            .AsNoTracking()
            .Where(r => r.ProviderId == providerId.Value);

        // Apply filters
        if (minRating.HasValue)
        {
            query = query.Where(r => r.RatingValue >= minRating.Value);
        }

        if (maxRating.HasValue)
        {
            query = query.Where(r => r.RatingValue <= maxRating.Value);
        }

        if (verifiedOnly.HasValue && verifiedOnly.Value)
        {
            query = query.Where(r => r.IsVerified);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy.ToLowerInvariant() switch
        {
            "rating" => sortDescending
                ? query.OrderByDescending(r => r.RatingValue).ThenByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.RatingValue).ThenBy(r => r.CreatedAt),
            "helpful" => sortDescending
                ? query.OrderByDescending(r => r.HelpfulCount).ThenByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.HelpfulCount).ThenBy(r => r.CreatedAt),
            _ => sortDescending // "date" or default
                ? query.OrderByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.CreatedAt)
        };

        // Apply pagination
        var reviews = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        _logger.LogDebug(
            "Retrieved {Count} reviews for Provider {ProviderId} (Page {Page}/{TotalPages})",
            reviews.Count,
            providerId.Value,
            pageNumber,
            totalPages);

        return new PaginatedReviews(
            Reviews: reviews,
            TotalCount: totalCount,
            PageNumber: pageNumber,
            PageSize: pageSize,
            TotalPages: totalPages);
    }

    public async Task<IReadOnlyList<Review>> GetByCustomerIdAsync(
        UserId customerId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        return await DbSet
            .AsNoTracking()
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReviewStatistics> GetReviewStatisticsAsync(
        ProviderId providerId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await DbSet
            .AsNoTracking()
            .Where(r => r.ProviderId == providerId.Value)
            .ToListAsync(cancellationToken);

        if (reviews.Count == 0)
        {
            return new ReviewStatistics(
                TotalReviews: 0,
                VerifiedReviews: 0,
                AverageRating: 0,
                FiveStarCount: 0,
                FourStarCount: 0,
                ThreeStarCount: 0,
                TwoStarCount: 0,
                OneStarCount: 0,
                ReviewsWithComments: 0,
                ReviewsWithProviderResponse: 0,
                MostRecentReviewDate: null,
                OldestReviewDate: null);
        }

        var verifiedReviews = reviews.Where(r => r.IsVerified).ToList();
        var averageRating = reviews.Average(r => r.RatingValue);

        // Count by rating (rounded down to nearest integer)
        var fiveStarCount = reviews.Count(r => r.RatingValue >= 4.5m);
        var fourStarCount = reviews.Count(r => r.RatingValue >= 3.5m && r.RatingValue < 4.5m);
        var threeStarCount = reviews.Count(r => r.RatingValue >= 2.5m && r.RatingValue < 3.5m);
        var twoStarCount = reviews.Count(r => r.RatingValue >= 1.5m && r.RatingValue < 2.5m);
        var oneStarCount = reviews.Count(r => r.RatingValue < 1.5m);

        var reviewsWithComments = reviews.Count(r => !string.IsNullOrWhiteSpace(r.Comment));
        var reviewsWithProviderResponse = reviews.Count(r => !string.IsNullOrWhiteSpace(r.ProviderResponse));

        var mostRecentReviewDate = reviews.Max(r => r.CreatedAt);
        var oldestReviewDate = reviews.Min(r => r.CreatedAt);

        _logger.LogDebug(
            "Calculated review statistics for Provider {ProviderId}: {Total} reviews, {Average:F2}★ average",
            providerId.Value,
            reviews.Count,
            averageRating);

        return new ReviewStatistics(
            TotalReviews: reviews.Count,
            VerifiedReviews: verifiedReviews.Count,
            AverageRating: Math.Round(averageRating, 2),
            FiveStarCount: fiveStarCount,
            FourStarCount: fourStarCount,
            ThreeStarCount: threeStarCount,
            TwoStarCount: twoStarCount,
            OneStarCount: oneStarCount,
            ReviewsWithComments: reviewsWithComments,
            ReviewsWithProviderResponse: reviewsWithProviderResponse,
            MostRecentReviewDate: mostRecentReviewDate,
            OldestReviewDate: oldestReviewDate);
    }

    public async Task<bool> HasReviewAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .AnyAsync(r => r.BookingId == bookingId, cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetRecentReviewsAsync(
        int count = 10,
        bool verifiedOnly = true,
        CancellationToken cancellationToken = default)
    {
        if (count < 1) count = 10;
        if (count > 100) count = 100;

        var query = DbSet.AsNoTracking();

        if (verifiedOnly)
        {
            query = query.Where(r => r.IsVerified);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}

using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Policies;
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
        CancellationToken cancellationToken = default,
        bool publishedOnly = true)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = DbSet
            .AsNoTracking()
            .Where(r => r.ProviderId == providerId.Value);

        // Fail closed: every public caller gets published reviews without having to ask. Only the owner-scoped
        // inbox opts out, explicitly.
        if (publishedOnly)
        {
            query = query.Where(r => r.ModerationStatus == ReviewModerationStatus.Published);
        }

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
            // Baseline plus live votes, spelled out: Review.HelpfulCount is the same sum but computed, so EF
            // cannot translate it. Ordering on the frozen baseline alone would pin the sort at deploy day.
            "helpful" => sortDescending
                ? query.OrderByDescending(r => r.LegacyHelpfulCount + r.HelpfulVoteCount).ThenByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.LegacyHelpfulCount + r.HelpfulVoteCount).ThenBy(r => r.CreatedAt),
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

    public async Task<PaginatedReviews> GetModerationQueueAsync(
        ReviewModerationFilter filter,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var reviews = DbSet.AsNoTracking();
        IOrderedQueryable<Review> query = filter switch
        {
            // Anything awaiting a decision — the review itself, or the provider's reply to it — ordered by how long
            // it has been waiting: an edited review waits from its edit, a reply from when it was written.
            ReviewModerationFilter.Pending => reviews
                .Where(r => r.ModerationStatus == ReviewModerationStatus.Pending
                            || r.ReplyModerationStatus == ReviewModerationStatus.Pending)
                .OrderBy(r => r.ModerationStatus == ReviewModerationStatus.Pending
                    ? (r.EditedAt ?? r.CreatedAt)
                    : (r.ProviderResponseAt ?? r.CreatedAt)),

            ReviewModerationFilter.Hidden => reviews
                .Where(r => r.ModerationStatus == ReviewModerationStatus.Hidden)
                .OrderByDescending(r => r.ModeratedAt),

            // Published reviews that someone has reported: they stay public until an administrator acts.
            ReviewModerationFilter.Reported => reviews
                .Where(r => r.ModerationStatus == ReviewModerationStatus.Published
                            && Context.Set<ReviewReport>().Any(rep => rep.ReviewId == r.Id))
                .OrderByDescending(r => Context.Set<ReviewReport>().Count(rep => rep.ReviewId == r.Id)),

            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null),
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var page = await query.ThenBy(r => r.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedReviews(
            Reviews: page,
            TotalCount: totalCount,
            PageNumber: pageNumber,
            PageSize: pageSize,
            TotalPages: (int)Math.Ceiling((double)totalCount / pageSize));
    }

    public Task<int> CountAwaitingReplyAsync(ProviderId providerId, CancellationToken cancellationToken = default) =>
        DbSet
            .AsNoTracking()
            .Where(r => r.ProviderId == providerId.Value)
            .Where(r => r.ModerationStatus == ReviewModerationStatus.Published)
            .Where(r => r.ProviderResponse == null || r.ReplyModerationStatus == ReviewModerationStatus.Rejected)
            .CountAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, bool>> GetVotesByUserAsync(
        UserId userId,
        IReadOnlyCollection<Guid> reviewIds,
        CancellationToken cancellationToken = default)
    {
        if (reviewIds.Count == 0) return new Dictionary<Guid, bool>();
        return await Context.Set<ReviewVote>()
            .AsNoTracking()
            .Where(v => v.UserId == userId && reviewIds.Contains(v.ReviewId))
            .ToDictionaryAsync(v => v.ReviewId, v => v.IsHelpful, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, ReviewContext>> GetReviewContextsAsync(
        IReadOnlyCollection<Review> reviews,
        CancellationToken cancellationToken = default)
    {
        if (reviews.Count == 0) return new Dictionary<Guid, ReviewContext>();

        var providerIds = reviews.Select(r => r.ProviderId).Distinct().ToList();
        var bookingIds = reviews.Select(r => BookingId.From(r.BookingId)).Distinct().ToList();

        var providers = await Context.Providers.AsNoTracking()
            .Where(p => providerIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Profile.BusinessName, p.Profile.DisplayImageUrl })
            .ToListAsync(cancellationToken);

        var bookings = await Context.Bookings.AsNoTracking()
            .Where(b => bookingIds.Contains(b.Id))
            .Select(b => new { b.Id, b.ServiceId })
            .ToListAsync(cancellationToken);

        var serviceIds = bookings.Select(b => b.ServiceId).Distinct().ToList();
        var services = await Context.Services.AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name })
            .ToListAsync(cancellationToken);

        var providerById = providers.ToDictionary(p => p.Id);
        var serviceByBooking = bookings.ToDictionary(
            b => b.Id.Value,
            b => services.FirstOrDefault(s => s.Id == b.ServiceId)?.Name);

        return reviews.ToDictionary(
            r => r.Id,
            r => new ReviewContext(
                providerById.TryGetValue(r.ProviderId, out var p) ? p.BusinessName : string.Empty,
                providerById.TryGetValue(r.ProviderId, out var q) ? q.DisplayImageUrl : null,
                serviceByBooking.TryGetValue(r.BookingId, out var service) ? service : null));
    }

    public async Task<IReadOnlyList<ReviewReport>> GetReportsAsync(
        IReadOnlyCollection<Guid> reviewIds,
        CancellationToken cancellationToken = default)
    {
        if (reviewIds.Count == 0) return Array.Empty<ReviewReport>();
        return await Context.Set<ReviewReport>()
            .AsNoTracking()
            .Where(r => reviewIds.Contains(r.ReviewId))
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
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
        // Published only. This block ships on the public listing; before moderation it averaged every review in
        // every state, which would now leak pending, rejected and hidden reviews into the public numbers.
        var reviews = await DbSet
            .AsNoTracking()
            .Where(r => r.ProviderId == providerId.Value && r.ModerationStatus == ReviewModerationStatus.Published)
            .ToListAsync(cancellationToken);

        // The same rule the provider's stored rating comes from, so the two can never disagree.
        var rating = ProviderRatingCalculator.Compute(reviews.Select(r => new ReviewRatingSnapshot(
            r.ModerationStatus, r.RatingValue, r.CleanlinessRating, r.SkillRating, r.PunctualityRating, r.ConductRating)));

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
                OldestReviewDate: null,
                Cleanliness: DimensionRating.None,
                Skill: DimensionRating.None,
                Punctuality: DimensionRating.None,
                Conduct: DimensionRating.None);
        }

        return new ReviewStatistics(
            TotalReviews: reviews.Count,
            VerifiedReviews: reviews.Count(r => r.IsVerified),
            AverageRating: rating.Average,
            FiveStarCount: reviews.Count(r => r.RatingValue >= 4.5m),
            FourStarCount: reviews.Count(r => r.RatingValue >= 3.5m && r.RatingValue < 4.5m),
            ThreeStarCount: reviews.Count(r => r.RatingValue >= 2.5m && r.RatingValue < 3.5m),
            TwoStarCount: reviews.Count(r => r.RatingValue >= 1.5m && r.RatingValue < 2.5m),
            OneStarCount: reviews.Count(r => r.RatingValue < 1.5m),
            ReviewsWithComments: reviews.Count(r => !string.IsNullOrWhiteSpace(r.Comment)),
            // Only replies the public can actually see.
            ReviewsWithProviderResponse: reviews.Count(r => r.IsReplyPubliclyVisible),
            MostRecentReviewDate: reviews.Max(r => r.CreatedAt),
            OldestReviewDate: reviews.Min(r => r.CreatedAt),
            Cleanliness: rating.Cleanliness,
            Skill: rating.Skill,
            Punctuality: rating.Punctuality,
            Conduct: rating.Conduct);
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

        // A public feed: published reviews only, like every other public read. (No caller today; kept closed so the
        // first one cannot leak the moderation queue.)
        var query = DbSet.AsNoTracking().Where(r => r.ModerationStatus == ReviewModerationStatus.Published);

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

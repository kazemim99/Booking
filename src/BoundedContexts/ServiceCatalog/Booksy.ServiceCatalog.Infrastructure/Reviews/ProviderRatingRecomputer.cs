using Booksy.ServiceCatalog.Application.Services.Reviews;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Policies;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Booksy.ServiceCatalog.Infrastructure.Reviews
{
    /// <inheritdoc cref="IProviderRatingRecomputer"/>
    public sealed class ProviderRatingRecomputer : IProviderRatingRecomputer
    {
        private readonly ServiceCatalogDbContext _context;

        public ProviderRatingRecomputer(ServiceCatalogDbContext context) => _context = context;

        public async Task RecomputeAsync(ProviderId providerId, CancellationToken cancellationToken = default)
        {
            var rating = ProviderRatingCalculator.Compute(await CurrentRatingsAsync(providerId, cancellationToken));

            var provider = await _context.Providers.FirstAsync(p => p.Id == providerId, cancellationToken);
            provider.SetRatingAggregates(rating.Average, rating.PublishedCount);

            var summary = await _context.ProviderRatingSummaries
                .FirstOrDefaultAsync(s => s.ProviderId == providerId.Value, cancellationToken);
            if (summary is null)
            {
                summary = ProviderRatingSummary.For(providerId.Value);
                _context.ProviderRatingSummaries.Add(summary);
            }

            summary.Overwrite(
                ToAggregate(rating.Cleanliness),
                ToAggregate(rating.Skill),
                ToAggregate(rating.Punctuality),
                ToAggregate(rating.Conduct),
                DateTime.UtcNow);
        }

        public async Task<int> RecomputeAllAsync(CancellationToken cancellationToken = default)
        {
            var providerIds = await _context.Providers.AsNoTracking().Select(p => p.Id).ToListAsync(cancellationToken);
            foreach (var providerId in providerIds)
                await RecomputeAsync(providerId, cancellationToken);
            return providerIds.Count;
        }

        /// <summary>
        /// The provider's reviews as this unit of work currently sees them: what the database stored, with every
        /// review this context is tracking taken from memory instead. Without the overlay, the approve or hide
        /// that triggered the recompute would not be in it yet.
        /// </summary>
        private async Task<IReadOnlyList<ReviewRatingSnapshot>> CurrentRatingsAsync(
            ProviderId providerId, CancellationToken cancellationToken)
        {
            var tracked = _context.ChangeTracker.Entries<Review>()
                .Where(e => e.Entity.ProviderId == providerId)
                .ToList();
            var trackedIds = tracked.Select(e => e.Entity.Id).ToList();

            var stored = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.ProviderId == providerId
                            && r.ModerationStatus == ReviewModerationStatus.Published
                            && !trackedIds.Contains(r.Id))
                .Select(r => new ReviewRatingSnapshot(
                    r.ModerationStatus, r.RatingValue,
                    r.CleanlinessRating, r.SkillRating, r.PunctualityRating, r.ConductRating))
                .ToListAsync(cancellationToken);

            var inMemory = tracked
                .Where(e => e.State is not (EntityState.Deleted or EntityState.Detached))
                .Select(e => e.Entity)
                .Select(r => new ReviewRatingSnapshot(
                    r.ModerationStatus, r.RatingValue,
                    r.CleanlinessRating, r.SkillRating, r.PunctualityRating, r.ConductRating));

            return stored.Concat(inMemory).ToList();
        }

        private static DimensionAggregate ToAggregate(DimensionRating rating) => new(rating.Average, rating.Count);
    }
}

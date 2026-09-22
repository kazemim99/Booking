using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories;

/// <summary>
/// Write repository implementation for Review aggregate
/// Handles review creation, updates, and deletions
/// </summary>
public sealed class ReviewWriteRepository
    : EfWriteRepositoryBase<Review, Guid, ServiceCatalogDbContext>,
      IReviewWriteRepository
{
    private readonly ILogger<ReviewWriteRepository> _logger;

    public ReviewWriteRepository(
        ServiceCatalogDbContext context,
        ILogger<ReviewWriteRepository> logger)
        : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Review?> GetByIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
    }

    public async Task<Review?> GetByBookingIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(r => r.BookingId == bookingId, cancellationToken);
    }

    public async Task SaveAsync(
        Review review,
        CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(review, cancellationToken);

        _logger.LogDebug(
            "Saved new review {ReviewId} for Provider {ProviderId} with rating {Rating}★",
            review.Id,
            review.ProviderId.Value,
            review.RatingValue);
    }

    public async Task UpdateAsync(
        Review review,
        CancellationToken cancellationToken = default)
    {
        Context.Update(review);

        _logger.LogDebug(
            "Updated review {ReviewId}",
            review.Id);

        await Task.CompletedTask;
    }

    public async Task DeleteAsync(
        Review review,
        CancellationToken cancellationToken = default)
    {
        DbSet.Remove(review);

        _logger.LogDebug(
            "Deleted review {ReviewId}",
            review.Id);

        await Task.CompletedTask;
    }

    public async Task<bool> HasReviewAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(r => r.BookingId == bookingId, cancellationToken);
    }

    public Task<ReviewVote?> GetVoteAsync(Guid reviewId, UserId userId, CancellationToken cancellationToken = default) =>
        Context.ReviewVotes.FirstOrDefaultAsync(v => v.ReviewId == reviewId && v.UserId == userId, cancellationToken);

    public async Task AddVoteAsync(ReviewVote vote, CancellationToken cancellationToken = default) =>
        await Context.ReviewVotes.AddAsync(vote, cancellationToken);

    public void RemoveVote(ReviewVote vote) => Context.ReviewVotes.Remove(vote);

    public Task AdjustVoteTalliesAsync(
        Guid reviewId, int helpfulDelta, int notHelpfulDelta, CancellationToken cancellationToken = default) =>
        // One UPDATE ... SET x = x + @delta: the row lock it takes serialises concurrent voters on this review,
        // and the unique (ReviewId, UserId) index turns a racing duplicate vote into a rolled-back transaction —
        // its delta with it.
        DbSet.Where(r => r.Id == reviewId).ExecuteUpdateAsync(s => s
                .SetProperty(r => r.HelpfulVoteCount, r => r.HelpfulVoteCount + helpfulDelta)
                .SetProperty(r => r.NotHelpfulVoteCount, r => r.NotHelpfulVoteCount + notHelpfulDelta),
            cancellationToken);

    public Task<bool> HasReportedAsync(Guid reviewId, UserId reporter, CancellationToken cancellationToken = default) =>
        Context.ReviewReports.AnyAsync(r => r.ReviewId == reviewId && r.ReportedByUserId == reporter, cancellationToken);

    public async Task AddReportAsync(ReviewReport report, CancellationToken cancellationToken = default) =>
        await Context.ReviewReports.AddAsync(report, cancellationToken);
}

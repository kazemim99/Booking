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
}

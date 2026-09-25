using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Repositories;

/// <summary>
/// Write repository for managing Review aggregate
/// Handles review creation, updates, and helpfulness tracking
/// </summary>
public interface IReviewWriteRepository : IWriteRepository<Review, Guid>
{
    /// <summary>
    /// Get review by ID for updating
    /// </summary>
    Task<Review?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get review by booking ID for validation (ensure one review per booking)
    /// </summary>
    Task<Review?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a new review
    /// </summary>
    Task SaveAsync(Review review, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing review
    /// </summary>
    Task UpdateAsync(Review review, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a review (soft delete via IsDeleted flag in base entity)
    /// </summary>
    Task DeleteAsync(Review review, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a booking already has a review
    /// </summary>
    Task<bool> HasReviewAsync(Guid bookingId, CancellationToken cancellationToken = default);

    // ── Votes ──

    /// <summary>The vote this user holds on this review, tracked, or null.</summary>
    Task<ReviewVote?> GetVoteAsync(Guid reviewId, UserId userId, CancellationToken cancellationToken = default);

    Task AddVoteAsync(ReviewVote vote, CancellationToken cancellationToken = default);

    void RemoveVote(ReviewVote vote);

    /// <summary>
    /// Moves the review's live tallies by these deltas in ONE atomic SQL statement, immediately, on the current
    /// transaction. Atomic because a read-modify-write of the counters loses updates under concurrency, and the
    /// review's Version token does not protect them (it only moves when a domain event is raised).
    /// </summary>
    Task AdjustVoteTalliesAsync(Guid reviewId, int helpfulDelta, int notHelpfulDelta, CancellationToken cancellationToken = default);

    // ── Reports ──

    Task<bool> HasReportedAsync(Guid reviewId, UserId reporter, CancellationToken cancellationToken = default);

    Task AddReportAsync(ReviewReport report, CancellationToken cancellationToken = default);
}

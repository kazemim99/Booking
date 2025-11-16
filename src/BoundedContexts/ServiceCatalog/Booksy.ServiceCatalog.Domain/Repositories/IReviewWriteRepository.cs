using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories;

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
}

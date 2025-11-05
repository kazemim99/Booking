// ========================================
// Booksy.ServiceCatalog.Domain/Repositories/IBookingWriteRepository.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    /// <summary>
    /// Write repository for Booking aggregate - optimized for commands
    /// </summary>
    public interface IBookingWriteRepository : IWriteRepository<Booking, BookingId>
    {
        /// <summary>
        /// Get booking by ID for updates (with tracking)
        /// </summary>
        Task<Booking?> GetByIdAsync(BookingId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Save a new booking
        /// </summary>
        Task SaveBookingAsync(Booking booking, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update an existing booking
        /// </summary>
        Task UpdateBookingAsync(Booking booking, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete a booking (soft delete)
        /// </summary>
        Task DeleteBookingAsync(Booking booking, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if booking exists for optimistic concurrency
        /// </summary>
        Task<bool> ExistsAsync(BookingId id, CancellationToken cancellationToken = default);
    }
}

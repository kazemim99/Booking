// ========================================
// Booksy.ServiceCatalog.Domain/Repositories/IPaymentWriteRepository.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    /// <summary>
    /// Write repository for Payment aggregate - optimized for commands
    /// </summary>
    public interface IPaymentWriteRepository : IWriteRepository<Payment, PaymentId>
    {
        /// <summary>
        /// Get payment by ID for updates (with tracking)
        /// </summary>
        Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get payment by booking ID
        /// </summary>
        Task<Payment?> GetByBookingIdAsync(BookingId bookingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get payment by payment intent ID
        /// </summary>
        Task<Payment?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get payment by ZarinPal authority code
        /// </summary>
        Task<Payment?> GetByAuthorityAsync(string authority, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a new payment
        /// </summary>
        Task AddAsync(Payment payment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update an existing payment
        /// </summary>
        Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if payment exists
        /// </summary>
        Task<bool> ExistsAsync(PaymentId id, CancellationToken cancellationToken = default);
    }
}

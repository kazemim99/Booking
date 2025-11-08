// ========================================
// Booksy.ServiceCatalog.Domain/Repositories/IPaymentReadRepository.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    /// <summary>
    /// Read repository for Payment aggregate - optimized for queries
    /// </summary>
    public interface IPaymentReadRepository : IReadRepository<Payment, PaymentId>
    {
        /// <summary>
        /// Get payment by ID (no tracking)
        /// </summary>
        Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all payments for a customer
        /// </summary>
        Task<IReadOnlyList<Payment>> GetByCustomerIdAsync(
            UserId customerId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all payments for a provider
        /// </summary>
        Task<IReadOnlyList<Payment>> GetByProviderIdAsync(
            ProviderId providerId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get payments by booking ID
        /// </summary>
        Task<IReadOnlyList<Payment>> GetByBookingIdAsync(
            BookingId bookingId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get payments by status
        /// </summary>
        Task<IReadOnlyList<Payment>> GetByStatusAsync(
            PaymentStatus status,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get payments in date range
        /// </summary>
        Task<IReadOnlyList<Payment>> GetByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get provider payments in date range (for payout calculation)
        /// </summary>
        Task<IReadOnlyList<Payment>> GetProviderPaymentsInRangeAsync(
            ProviderId providerId,
            DateTime startDate,
            DateTime endDate,
            PaymentStatus? status = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get payment by ZarinPal authority code (read-only)
        /// </summary>
        Task<Payment?> GetByAuthorityAsync(string authority, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get failed payments since a specific date (for retry)
        /// </summary>
        Task<IReadOnlyList<Payment>> GetFailedPaymentsAsync(
            DateTime fromDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get customer payment history with pagination
        /// </summary>
        Task<(IReadOnlyList<Payment> Payments, int TotalCount)> GetCustomerPaymentHistoryAsync(
            UserId customerId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int skip = 0,
            int take = 20,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get provider revenue statistics for a date range
        /// </summary>
        Task<(decimal TotalRevenue, decimal TotalRefunds, int SuccessfulPayments, int TotalPayments)> GetProviderRevenueStatsAsync(
            ProviderId providerId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get payments for reconciliation (completed payments in date range)
        /// </summary>
        Task<IReadOnlyList<Payment>> GetPaymentsForReconciliationAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);
    }
}

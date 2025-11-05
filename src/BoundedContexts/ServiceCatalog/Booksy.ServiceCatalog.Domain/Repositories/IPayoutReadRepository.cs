// ========================================
// Booksy.ServiceCatalog.Domain/Repositories/IPayoutReadRepository.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.PayoutAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    /// <summary>
    /// Read repository for Payout aggregate - optimized for queries
    /// </summary>
    public interface IPayoutReadRepository : IReadRepository<Payout, PayoutId>
    {
        /// <summary>
        /// Get payout by ID (no tracking)
        /// </summary>
        Task<Payout?> GetByIdAsync(PayoutId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all payouts for a provider
        /// </summary>
        Task<IReadOnlyList<Payout>> GetByProviderIdAsync(
            ProviderId providerId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get payouts by status
        /// </summary>
        Task<IReadOnlyList<Payout>> GetByStatusAsync(
            PayoutStatus status,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get pending payouts (scheduled for processing)
        /// </summary>
        Task<IReadOnlyList<Payout>> GetPendingPayoutsAsync(
            DateTime? beforeDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get payouts in date range
        /// </summary>
        Task<IReadOnlyList<Payout>> GetByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get provider payouts in date range
        /// </summary>
        Task<IReadOnlyList<Payout>> GetProviderPayoutsInRangeAsync(
            ProviderId providerId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);
    }
}

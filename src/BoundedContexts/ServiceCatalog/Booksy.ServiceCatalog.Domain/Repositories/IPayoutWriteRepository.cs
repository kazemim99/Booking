// ========================================
// Booksy.ServiceCatalog.Domain/Repositories/IPayoutWriteRepository.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.PayoutAggregate;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    /// <summary>
    /// Write repository for Payout aggregate - optimized for commands
    /// </summary>
    public interface IPayoutWriteRepository : IWriteRepository<Payout, PayoutId>
    {
        /// <summary>
        /// Get payout by ID for updates (with tracking)
        /// </summary>
        Task<Payout?> GetByIdAsync(PayoutId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a new payout
        /// </summary>
        Task AddAsync(Payout payout, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update an existing payout
        /// </summary>
        Task UpdateAsync(Payout payout, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if payout exists
        /// </summary>
        Task<bool> ExistsAsync(PayoutId id, CancellationToken cancellationToken = default);
    }
}

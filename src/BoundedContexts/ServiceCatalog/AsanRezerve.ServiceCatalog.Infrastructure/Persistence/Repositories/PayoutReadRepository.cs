// ========================================
// AsanRezerve.ServiceCatalog.Infrastructure/Persistence/Repositories/PayoutReadRepository.cs
// ========================================
using AsanRezerve.Infrastructure.Core.Persistence.Base;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PayoutAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class PayoutReadRepository : EfReadRepositoryBase<Payout, PayoutId, ServiceCatalogDbContext>, IPayoutReadRepository
    {
        public PayoutReadRepository(ServiceCatalogDbContext context)
            : base(context)
        {
        }

        public new async Task<Payout?> GetByIdAsync(PayoutId id, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Payout>> GetByProviderIdAsync(
            ProviderId providerId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Where(p => p.ProviderId == providerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Payout>> GetByStatusAsync(
            PayoutStatus status,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Payout>> GetPendingPayoutsAsync(
            DateTime? beforeDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = DbSet
                .AsNoTracking()
                .Where(p => p.Status == PayoutStatus.Pending);

            if (beforeDate.HasValue)
            {
                query = query.Where(p => p.ScheduledAt <= beforeDate.Value);
            }

            return await query
                .OrderBy(p => p.ScheduledAt ?? p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Payout>> GetByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Payout>> GetProviderPayoutsInRangeAsync(
            ProviderId providerId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Where(p => p.ProviderId == providerId
                            && p.CreatedAt >= startDate
                            && p.CreatedAt <= endDate)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}

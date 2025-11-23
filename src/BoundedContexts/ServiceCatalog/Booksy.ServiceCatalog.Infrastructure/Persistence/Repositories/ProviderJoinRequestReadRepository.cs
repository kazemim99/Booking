using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class ProviderJoinRequestReadRepository : EfReadRepositoryBase<ProviderJoinRequest, Guid, ServiceCatalogDbContext>, IProviderJoinRequestReadRepository
    {
        public ProviderJoinRequestReadRepository(
            ServiceCatalogDbContext context,
            ILogger<ProviderJoinRequestReadRepository> logger)
            : base(context)
        {
        }

        public override async Task<ProviderJoinRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .FirstOrDefaultAsync(pjr => pjr.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<ProviderJoinRequest>> GetByOrganizationIdAsync(
            ProviderId organizationId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(pjr => pjr.OrganizationId == organizationId)
                .OrderByDescending(pjr => pjr.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ProviderJoinRequest>> GetByOrganizationIdAndStatusAsync(
            ProviderId organizationId,
            JoinRequestStatus status,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(pjr => pjr.OrganizationId == organizationId && pjr.Status == status)
                .OrderByDescending(pjr => pjr.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ProviderJoinRequest>> GetByRequesterIdAsync(
            ProviderId requesterId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(pjr => pjr.RequesterId == requesterId)
                .OrderByDescending(pjr => pjr.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<ProviderJoinRequest?> GetPendingByRequesterAndOrganizationAsync(
            ProviderId requesterId,
            ProviderId organizationId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .FirstOrDefaultAsync(pjr =>
                    pjr.RequesterId == requesterId &&
                    pjr.OrganizationId == organizationId &&
                    pjr.Status == JoinRequestStatus.Pending,
                    cancellationToken);
        }

        public async Task<int> CountPendingByOrganizationAsync(
            ProviderId organizationId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .CountAsync(pjr => pjr.OrganizationId == organizationId && pjr.Status == JoinRequestStatus.Pending, cancellationToken);
        }

        public async Task<bool> HasPendingRequestAsync(
            ProviderId requesterId,
            ProviderId organizationId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AnyAsync(pjr =>
                    pjr.RequesterId == requesterId &&
                    pjr.OrganizationId == organizationId &&
                    pjr.Status == JoinRequestStatus.Pending,
                    cancellationToken);
        }
    }
}

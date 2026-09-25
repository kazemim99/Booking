using AsanRezerve.Infrastructure.Core.Persistence.Base;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class ProviderInvitationReadRepository : EfReadRepositoryBase<ProviderInvitation, Guid, ServiceCatalogDbContext>, IProviderInvitationReadRepository
    {
        public ProviderInvitationReadRepository(
            ServiceCatalogDbContext context,
            ILogger<ProviderInvitationReadRepository> logger)
            : base(context)
        {
        }

        public override async Task<ProviderInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .FirstOrDefaultAsync(pi => pi.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<ProviderInvitation>> GetByOrganizationIdAsync(
            ProviderId organizationId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(pi => pi.OrganizationId == organizationId)
                .OrderByDescending(pi => pi.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ProviderInvitation>> GetByOrganizationIdAndStatusAsync(
            ProviderId organizationId,
            InvitationStatus status,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(pi => pi.OrganizationId == organizationId && pi.Status == status)
                .OrderByDescending(pi => pi.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<ProviderInvitation?> GetByPhoneNumberAndOrganizationAsync(
            string phoneNumber,
            ProviderId organizationId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .FirstOrDefaultAsync(pi =>
                    pi.PhoneNumber.Value == phoneNumber &&
                    pi.OrganizationId == organizationId &&
                    pi.Status == InvitationStatus.Pending,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<ProviderInvitation>> GetPendingByPhoneNumberAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(pi => pi.PhoneNumber.Value == phoneNumber && pi.Status == InvitationStatus.Pending)
                .OrderByDescending(pi => pi.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ProviderInvitation>> GetExpiredInvitationsAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await DbSet
                .Where(pi => pi.Status == InvitationStatus.Pending && pi.ExpiresAt < now)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountPendingByOrganizationAsync(
            ProviderId organizationId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .CountAsync(pi => pi.OrganizationId == organizationId && pi.Status == InvitationStatus.Pending, cancellationToken);
        }
    }
}

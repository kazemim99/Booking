using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class OrganizationMembershipRepository
        : EfWriteRepositoryBase<OrganizationMembership, Guid, ServiceCatalogDbContext>, IOrganizationMembershipRepository
    {
        public OrganizationMembershipRepository(ServiceCatalogDbContext context) : base(context) { }

        public async Task<OrganizationMembership?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(m => m.StaffProfile)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task<OrganizationMembership?> GetActiveByPersonAndOrganizationAsync(
            UserId personId, ProviderId organizationId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(m => m.StaffProfile)
                .FirstOrDefaultAsync(
                    m => m.PersonId == personId
                         && m.OrganizationId == organizationId
                         && m.Status != MembershipStatus.Terminated,
                    cancellationToken);
        }

        public async Task<bool> HasActiveMembershipAsync(
            UserId personId, ProviderId organizationId, CancellationToken cancellationToken = default)
        {
            return await DbSet.AnyAsync(
                m => m.PersonId == personId
                     && m.OrganizationId == organizationId
                     && m.Status != MembershipStatus.Terminated,
                cancellationToken);
        }

        public async Task<IReadOnlyList<OrganizationMembership>> GetByOrganizationAsync(
            ProviderId organizationId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(m => m.StaffProfile)
                .Where(m => m.OrganizationId == organizationId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<OrganizationMembership>> GetByPersonAsync(
            UserId personId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(m => m.StaffProfile)
                .Where(m => m.PersonId == personId)
                .ToListAsync(cancellationToken);
        }

        public async Task SaveAsync(OrganizationMembership membership, CancellationToken cancellationToken = default)
        {
            // Persisted by the UnitOfWork (which also dispatches domain events); no SaveChanges here.
            await DbSet.AddAsync(membership, cancellationToken);
        }

        public new Task UpdateAsync(OrganizationMembership membership, CancellationToken cancellationToken = default)
        {
            DbSet.Update(membership);
            return Task.CompletedTask;
        }
    }
}

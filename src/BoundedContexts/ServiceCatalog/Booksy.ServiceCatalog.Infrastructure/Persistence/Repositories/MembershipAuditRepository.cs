using Booksy.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class MembershipAuditRepository : IMembershipAuditRepository
    {
        private readonly ServiceCatalogDbContext _context;

        public MembershipAuditRepository(ServiceCatalogDbContext context) => _context = context;

        public async Task AppendAsync(MembershipAuditEntry entry, CancellationToken cancellationToken = default)
        {
            // Tracked here; the UnitOfWork commits it with the change it describes, so
            // the trail can never diverge from what actually happened.
            await _context.Set<MembershipAuditEntry>().AddAsync(entry, cancellationToken);
        }

        public async Task<IReadOnlyList<MembershipAuditEntry>> GetForMembershipAsync(
            Guid membershipId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MembershipAuditEntry>()
                .AsNoTracking()
                .Where(e => e.MembershipId == membershipId)
                .OrderByDescending(e => e.OccurredAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<MembershipAuditEntry>> GetForOrganizationAsync(
            ProviderId organizationId, int limit = 100, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MembershipAuditEntry>()
                .AsNoTracking()
                .Where(e => e.OrganizationId == organizationId)
                .OrderByDescending(e => e.OccurredAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
    }
}

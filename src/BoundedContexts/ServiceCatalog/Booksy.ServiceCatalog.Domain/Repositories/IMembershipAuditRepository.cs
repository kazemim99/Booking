using Booksy.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    /// <summary>
    /// Append-only access to the membership audit trail. There is deliberately no
    /// update or delete: history is evidence, not state.
    /// </summary>
    public interface IMembershipAuditRepository
    {
        Task AppendAsync(MembershipAuditEntry entry, CancellationToken cancellationToken = default);

        /// <summary>Full history of one membership, newest first.</summary>
        Task<IReadOnlyList<MembershipAuditEntry>> GetForMembershipAsync(
            Guid membershipId, CancellationToken cancellationToken = default);

        /// <summary>Recent membership activity across an organization, newest first.</summary>
        Task<IReadOnlyList<MembershipAuditEntry>> GetForOrganizationAsync(
            ProviderId organizationId, int limit = 100, CancellationToken cancellationToken = default);
    }
}

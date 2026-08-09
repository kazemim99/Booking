using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    /// <summary>
    /// Read+write access to organization memberships. Combined (like IUserRepository)
    /// because command handlers routinely query the org's members to enforce the
    /// organization-wide invariants (one live membership per person/org; keep ≥1 owner)
    /// that a single membership aggregate cannot see on its own.
    /// </summary>
    public interface IOrganizationMembershipRepository
    {
        Task<OrganizationMembership?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>The person's current (non-terminated) membership in an organization, if any.</summary>
        Task<OrganizationMembership?> GetActiveByPersonAndOrganizationAsync(
            UserId personId, ProviderId organizationId, CancellationToken cancellationToken = default);

        /// <summary>True when the person already has a non-terminated membership in the organization.</summary>
        Task<bool> HasActiveMembershipAsync(
            UserId personId, ProviderId organizationId, CancellationToken cancellationToken = default);

        /// <summary>All memberships of an organization (its staff directory), including StaffProfiles.</summary>
        Task<IReadOnlyList<OrganizationMembership>> GetByOrganizationAsync(
            ProviderId organizationId, CancellationToken cancellationToken = default);

        /// <summary>All memberships a person holds across organizations (for the salon switcher).</summary>
        Task<IReadOnlyList<OrganizationMembership>> GetByPersonAsync(
            UserId personId, CancellationToken cancellationToken = default);

        Task SaveAsync(OrganizationMembership membership, CancellationToken cancellationToken = default);

        Task UpdateAsync(OrganizationMembership membership, CancellationToken cancellationToken = default);
    }
}

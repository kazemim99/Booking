using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Booksy.ServiceCatalog.Application.Queries.Membership.CanManageOrganization;

/// <inheritdoc cref="CanManageOrganizationQuery"/>
public sealed class CanManageOrganizationQueryHandler
    : IQueryHandler<CanManageOrganizationQuery, bool>
{
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IProviderReadRepository _providerRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CanManageOrganizationQueryHandler(
        IOrganizationMembershipRepository membershipRepository,
        IProviderReadRepository providerRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _membershipRepository = membershipRepository;
        _providerRepository = providerRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> Handle(
        CanManageOrganizationQuery request,
        CancellationToken cancellationToken)
    {
        var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userGuid))
            return false;

        var personId = UserId.From(userGuid);
        var organizationId = ProviderId.From(request.OrganizationId);

        var membership = await _membershipRepository.GetActiveByPersonAndOrganizationAsync(
            personId, organizationId, cancellationToken);

        if (membership is not null && Allows(membership.Roles, request.Permission))
            return true;

        // Provider.OwnerId is the migration-only fallback for organizations registered
        // before ownership moved onto memberships (FOLLOW-UPS #40). It grants everything,
        // because an owner with no membership row is a gap in the backfill, not a
        // downgrade in what they may do.
        var organization = await _providerRepository.GetByIdAsync(organizationId, cancellationToken);
        return organization is not null && personId.Equals(organization.OwnerId);
    }

    private static bool Allows(
        IReadOnlyCollection<MembershipRole> roles,
        OrganizationPermission permission) =>
        permission switch
        {
            // Running the salon. A stylist working here does not get to rewrite its hours.
            OrganizationPermission.ManageOrganization =>
                roles.Contains(MembershipRole.Owner) || roles.Contains(MembershipRole.Manager),

            // The day book. Any active member of the salon, whatever their role — an
            // active membership is itself the proof that this person works here.
            OrganizationPermission.ManageBookings => true,

            _ => false
        };
}

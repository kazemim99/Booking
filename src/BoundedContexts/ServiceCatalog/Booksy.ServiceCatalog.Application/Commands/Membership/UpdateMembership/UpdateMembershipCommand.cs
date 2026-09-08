using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Membership.UpdateMembership;

/// <summary>
/// Edit a member's organization-scoped details: the salon's display name for them
/// (unclaimed members only), their per-salon bio, and whether they currently provide
/// services to customers.
/// </summary>
/// <remarks>
/// Roles are deliberately NOT part of this command — <c>ChangeMembershipRolesCommand</c>
/// owns them, because changing roles carries the "keep ≥1 owner" invariant that needs to
/// see every membership in the organization.
///
/// Person-level fields (first/last name, email, phone) are not editable here at all: they
/// belong to the Person in UserManagement, not to the salon. See
/// <c>OrganizationMembership.UpdateStaffDetails</c>.
/// </remarks>
public sealed record UpdateMembershipCommand(
    Guid MembershipId,
    string? DisplayName = null,
    string? BioOverride = null,
    bool? ProvidesServices = null,
    string? PhotoUrl = null) : ICommand<UpdateMembershipResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record UpdateMembershipResult(
    Guid MembershipId,
    Guid OrganizationId,
    Guid? PersonId,
    string? DisplayName,
    string? BioOverride,
    string? PhotoUrl,
    bool ProvidesServices,
    IReadOnlyCollection<string> Roles);

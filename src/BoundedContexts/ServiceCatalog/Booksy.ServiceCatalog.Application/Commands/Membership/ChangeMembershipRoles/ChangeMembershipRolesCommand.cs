using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Membership.ChangeMembershipRoles;

/// <summary>
/// Replace a membership's organization-scoped roles (e.g. promote a stylist to
/// Manager, or make the owner also a StaffProvider). Owner-only. The organization
/// can never be left without an owner.
/// </summary>
public sealed record ChangeMembershipRolesCommand(Guid MembershipId, IReadOnlyList<string> Roles)
    : ICommand<ChangeMembershipRolesResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record ChangeMembershipRolesResult(
    Guid MembershipId,
    Guid OrganizationId,
    IReadOnlyCollection<string> Roles,
    bool ProvidesServices);

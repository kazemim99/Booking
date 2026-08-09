using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Membership.AcceptInvitationAsMember;

/// <summary>
/// An already-registered person accepts an invitation and becomes an active member of
/// the organization. The accepting person is the authenticated caller; their account is
/// reused (never duplicated), and they must own the phone the invitation was sent to.
/// </summary>
public sealed record AcceptInvitationAsMemberCommand(Guid InvitationId)
    : ICommand<AcceptInvitationAsMemberResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record AcceptInvitationAsMemberResult(
    Guid InvitationId,
    Guid OrganizationId,
    Guid MembershipId,
    Guid PersonId,
    IReadOnlyCollection<string> Roles,
    DateTime AcceptedAt);

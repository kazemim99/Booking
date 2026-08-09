using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Membership.RevokeInvitation;

/// <summary>
/// An organization withdraws a pending invitation (wrong number, changed mind,
/// candidate declined verbally). Owner-only. The invitation is closed, not deleted:
/// the attempt stays in the audit trail.
/// </summary>
public sealed record RevokeInvitationCommand(Guid InvitationId, string? Reason)
    : ICommand<RevokeInvitationResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record RevokeInvitationResult(
    Guid InvitationId,
    Guid OrganizationId,
    string Status,
    DateTime RevokedAt);

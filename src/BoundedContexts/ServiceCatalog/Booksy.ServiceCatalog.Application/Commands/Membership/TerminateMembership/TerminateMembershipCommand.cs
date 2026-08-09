using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Membership.TerminateMembership;

/// <summary>
/// Terminate a membership — the person leaves the organization (S5). Callable by an
/// owner of the organization (staff removal) or by the member themselves (leaving).
/// The person, their profile, history and reputation are untouched; only this
/// membership ends. An organization can never lose its last owner this way.
/// </summary>
public sealed record TerminateMembershipCommand(Guid MembershipId, string? Reason)
    : ICommand<TerminateMembershipResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record TerminateMembershipResult(
    Guid MembershipId,
    Guid OrganizationId,
    DateTime LeftAt);

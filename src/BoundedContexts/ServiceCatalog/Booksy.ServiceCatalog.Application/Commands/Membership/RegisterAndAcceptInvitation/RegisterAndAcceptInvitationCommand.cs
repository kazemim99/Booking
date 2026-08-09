using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Membership.RegisterAndAcceptInvitation;

/// <summary>
/// New-user path (S4): a person with no account opens an invitation SMS, enters
/// their name, and verifies the OTP sent to the invited phone. This creates (or
/// reuses, by phone) the account and an active membership — never a duplicate
/// person. The phone is taken from the invitation, not the request, so the OTP
/// proves ownership of exactly the invited number.
/// </summary>
public sealed record RegisterAndAcceptInvitationCommand(
    Guid InvitationId,
    string FirstName,
    string LastName,
    string? Email,
    string OtpCode) : ICommand<RegisterAndAcceptInvitationResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record RegisterAndAcceptInvitationResult(
    Guid PersonId,
    Guid MembershipId,
    Guid OrganizationId,
    bool IsNewAccount,
    IReadOnlyCollection<string> Roles,
    DateTime AcceptedAt);

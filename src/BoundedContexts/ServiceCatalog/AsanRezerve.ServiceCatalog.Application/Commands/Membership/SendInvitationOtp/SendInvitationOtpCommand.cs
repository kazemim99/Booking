using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Membership.SendInvitationOtp;

/// <summary>
/// Step 1 of the new-user register-and-accept flow (S4): sends an OTP to the
/// phone number an invitation was sent to, so an account-less invitee can prove
/// they own that number before <c>RegisterAndAcceptInvitationCommand</c> creates
/// their account. Anonymous by design — the invitee has no account yet — so the
/// phone number itself is never accepted from or returned to the caller; it is
/// resolved from the invitation server-side and only the masked form is echoed.
/// </summary>
public sealed record SendInvitationOtpCommand(Guid InvitationId) : ICommand<SendInvitationOtpResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record SendInvitationOtpResult(string MaskedPhoneNumber);

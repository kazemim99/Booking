namespace Booksy.ServiceCatalog.Api.Models.Requests;

/// <summary>
/// New-user invitation acceptance. The phone comes from the invitation itself;
/// the OTP proves the caller owns that number.
/// </summary>
public sealed record RegisterAndAcceptInvitationRequest(
    string FirstName,
    string LastName,
    string? Email,
    string OtpCode);

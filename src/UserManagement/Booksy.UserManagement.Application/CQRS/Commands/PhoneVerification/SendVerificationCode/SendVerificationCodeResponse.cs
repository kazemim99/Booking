namespace Booksy.UserManagement.Application.CQRS.Commands.PhoneVerification.SendVerificationCode;

/// <summary>
/// Response for SendVerificationCode command
/// </summary>
public sealed record SendVerificationCodeResponse(
    Guid VerificationId,
    string MaskedPhoneNumber,
    DateTime ExpiresAt,
    int MaxAttempts,
    string Message
);

namespace Booksy.UserManagement.Application.CQRS.Commands.SendVerificationCode;

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

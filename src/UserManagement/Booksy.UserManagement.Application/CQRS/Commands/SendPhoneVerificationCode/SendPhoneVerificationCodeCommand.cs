using MediatR;

namespace Booksy.UserManagement.Application.CQRS.Commands.SendPhoneVerificationCode;

/// <summary>
/// Command to send a verification code to a phone number
/// </summary>
public record SendPhoneVerificationCodeCommand(
    Guid UserId,
    string PhoneNumber
) : IRequest<SendPhoneVerificationCodeResult>;

/// <summary>
/// Result of sending verification code
/// </summary>
public record SendPhoneVerificationCodeResult(
    bool Success,
    string Message,
    DateTime ExpiresAt
);

using MediatR;

namespace Booksy.UserManagement.Application.CQRS.Commands.VerifyPhoneCode;

/// <summary>
/// Command to verify a phone number with the sent code
/// </summary>
public record VerifyPhoneCodeCommand(
    Guid UserId,
    string PhoneNumber,
    string VerificationCode
) : IRequest<VerifyPhoneCodeResult>;

/// <summary>
/// Result of phone verification
/// </summary>
public record VerifyPhoneCodeResult(
    bool Success,
    string Message,
    string? PhoneNumber = null,
    DateTime? VerifiedAt = null
);

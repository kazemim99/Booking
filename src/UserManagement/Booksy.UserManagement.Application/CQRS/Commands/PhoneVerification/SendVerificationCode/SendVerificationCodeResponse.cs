namespace Booksy.UserManagement.Application.CQRS.Commands.PhoneVerification.SendVerificationCode;

public record SendVerificationCodeResponse(
    bool Success,
    string Message,
    string MaskedPhoneNumber,
    int ExpiresIn
);

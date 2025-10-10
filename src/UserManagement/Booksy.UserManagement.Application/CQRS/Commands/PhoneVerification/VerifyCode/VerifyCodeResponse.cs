namespace Booksy.UserManagement.Application.CQRS.Commands.PhoneVerification.VerifyCode;

public record VerifyCodeResponse(
    bool Success,
    string? AccessToken,
    string? RefreshToken,
    int? ExpiresIn,
    UserInfo? User,
    string? ErrorMessage = null,
    int? RemainingAttempts = null
);

public record UserInfo(
    Guid Id,
    string PhoneNumber,
    bool PhoneVerified,
    string UserType,
    string Status,
    string[] Roles
);

namespace Booksy.UserManagement.Application.CQRS.Commands.PhoneVerification.VerifyCode;

/// <summary>
/// Response for VerifyCode command
/// </summary>
public sealed record VerifyCodeResponse(
    bool Success,
    string Message,
    string? ErrorMessage = null,
    int? RemainingAttempts = null,
    UserDto? User = null,
    string? AccessToken = null,
    string? RefreshToken = null,
    DateTime? ExpiresAt = null
);

/// <summary>
/// User DTO for verification response
/// </summary>
public sealed record UserDto(
    Guid Id,
    string PhoneNumber,
    string? Email = null,
    string? FirstName = null,
    string? LastName = null,
    string UserType = "Provider"
);

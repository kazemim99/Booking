// ========================================
// AsanRezerve.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
// ========================================
// AsanRezerve.UserManagement.Application/Commands/AuthenticateUser/AuthenticateUserResult.cs
// ========================================
namespace AsanRezerve.UserManagement.Application.CQRS.Commands.AuthenticateUser
{
    public sealed record AuthenticateUserResult(
        Guid UserId,
        string Email,
        string DisplayName,
        List<string> Roles,
        string AccessToken,
        string RefreshToken,
        int ExpiresIn);
}


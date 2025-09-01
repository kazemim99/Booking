// ========================================
// Booksy.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
namespace Booksy.UserManagement.Application.CQRS.Commands.AuthenticateUser
{
    public sealed record AuthenticateUserCommand(
        string Email,
        string Password,
        bool RememberMe = false,
        string? TwoFactorCode = null,
        string? IpAddress = null,
        string? UserAgent = null,
        Guid? IdempotencyKey = null) : ICommand<AuthenticateUserResult>;
}


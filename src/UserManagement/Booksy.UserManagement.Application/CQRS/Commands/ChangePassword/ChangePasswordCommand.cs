// ========================================
// Booksy.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
namespace Booksy.UserManagement.Application.CQRS.Commands.ChangePassword
{
    public sealed record ChangePasswordCommand(
        Guid UserId,
        string CurrentPassword,
        string NewPassword,
        bool LogoutAllSessions = false,
        Guid? IdempotencyKey = null) : ICommand;
}



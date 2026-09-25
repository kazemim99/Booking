// ========================================
// AsanRezerve.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
namespace AsanRezerve.UserManagement.Application.CQRS.Commands.RequestPasswordReset
{
    public sealed record RequestPasswordResetCommand(
        string Email,
        string? IpAddress = null,
        Guid? IdempotencyKey = null) : ICommand;
}


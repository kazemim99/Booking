// ========================================
// AsanRezerve.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
namespace AsanRezerve.UserManagement.Application.CQRS.Commands.RefreshToken
{
    public sealed record RefreshTokenCommand(
        string RefreshToken,
        string? IpAddress = null,
        Guid? IdempotencyKey = null) : ICommand<RefreshTokenResult>;
}



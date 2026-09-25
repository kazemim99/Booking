// ========================================
// AsanRezerve.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================

// ========================================
// AsanRezerve.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.UserManagement.Application.CQRS.Commands.DeActivateUser
{
    /// <summary>
    /// Command to activate a user account
    /// </summary>
    public sealed record DeActivateUserCommand(
        Guid id,
        string ActivationToken,
        Guid? IdempotencyKey = null) : ICommand<DeActivateUserResult>;
}



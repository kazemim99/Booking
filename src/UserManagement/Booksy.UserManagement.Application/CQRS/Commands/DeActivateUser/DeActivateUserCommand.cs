// ========================================
// Booksy.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================

// ========================================
// Booksy.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Commands.ActivateUser
{
    /// <summary>
    /// Command to activate a user account
    /// </summary>
    public sealed record DeActivateUserCommand(
        Guid id,
        string ActivationToken,
        Guid? IdempotencyKey = null) : ICommand<DeActivateUserResult>;
}



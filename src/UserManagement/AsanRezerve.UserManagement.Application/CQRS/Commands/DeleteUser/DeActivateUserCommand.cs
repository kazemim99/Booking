// ========================================
// AsanRezerve.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================

// ========================================
// AsanRezerve.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================

// ========================================
// AsanRezerve.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================

// ========================================
// AsanRezerve.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.UserManagement.Application.CQRS.Commands.DeleteUser
{
    /// <summary>
    /// Command to activate a user account
    /// </summary>
    public sealed record DeleteUserCommand(
        Guid id,
        Guid? IdempotencyKey = null) : ICommand<DeleteUserResult>;
}



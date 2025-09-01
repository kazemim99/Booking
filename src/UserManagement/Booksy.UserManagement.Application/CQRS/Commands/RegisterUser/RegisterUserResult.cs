// ========================================
// Booksy.UserManagement.Application/Commands/RegisterUser/RegisterUserCommand.cs
// ========================================
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Application.CQRS.Commands.RegisterUser
{
    /// <summary>
    /// Result of user registration
    /// </summary>
    public sealed record RegisterUserResult(
        Guid UserId,
        string Email,
        string FullName,
        UserStatus Status,
        bool RequiresActivation,
        string? ActivationToken,
        DateTime? TokenExpiresAt);
}
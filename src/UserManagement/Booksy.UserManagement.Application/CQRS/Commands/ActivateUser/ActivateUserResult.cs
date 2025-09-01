// ========================================
// Booksy.UserManagement.Application/Commands/ActivateUser/ActivateUserResult.cs
// ========================================
namespace Booksy.UserManagement.Application.CQRS.Commands.ActivateUser
{
    public sealed record ActivateUserResult(
        Guid UserId,
        string Email,
        DateTime ActivatedAt);
}


// ========================================
// Booksy.UserManagement.Application/Commands/ActivateUser/ActivateUserResult.cs
// ========================================
namespace Booksy.UserManagement.Application.CQRS.Commands.DeActivateUser
{
    public sealed record DeActivateUserResult(
        Guid UserId,
        string Email,
        DateTime ActivatedAt);
}


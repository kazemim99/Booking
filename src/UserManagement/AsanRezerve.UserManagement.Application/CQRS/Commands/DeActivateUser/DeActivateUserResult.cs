// ========================================
// AsanRezerve.UserManagement.Application/Commands/ActivateUser/ActivateUserResult.cs
// ========================================
namespace AsanRezerve.UserManagement.Application.CQRS.Commands.DeActivateUser
{
    public sealed record DeActivateUserResult(
        Guid UserId,
        string Email,
        DateTime ActivatedAt);
}


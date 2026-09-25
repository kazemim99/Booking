// ========================================
// AsanRezerve.UserManagement.Domain/Events/UserRegisteredEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Domain.Enums;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record UserRegisteredEvent(
        UserId UserId,
        Email Email,
        string FirstName,
        string LastName,
        UserType UserType,
        DateTime RegisteredAt
    ) : DomainEvent("User", UserId.ToString());
}

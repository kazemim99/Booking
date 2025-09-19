// ========================================
// Booksy.UserManagement.Domain/Events/UserRegisteredEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Domain.Events
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

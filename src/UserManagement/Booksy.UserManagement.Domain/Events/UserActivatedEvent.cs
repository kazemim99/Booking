// ========================================
// Booksy.UserManagement.Domain/Events/UserActivatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record UserActivatedEvent(
        UserId UserId,
        Email Email,
        DateTime ActivatedAt
    ) : DomainEvent("User", UserId.ToString());
}

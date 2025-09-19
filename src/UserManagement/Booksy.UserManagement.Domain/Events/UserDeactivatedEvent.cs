// ========================================
// Booksy.UserManagement.Domain/Events/UserDeactivatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record UserDeactivatedEvent(
        UserId UserId,
        string Reason,
        DateTime DeactivatedAt
    ) : DomainEvent("User", UserId.ToString());
}

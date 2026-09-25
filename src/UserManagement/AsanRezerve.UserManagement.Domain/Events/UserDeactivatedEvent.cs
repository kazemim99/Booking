// ========================================
// AsanRezerve.UserManagement.Domain/Events/UserDeactivatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record UserDeactivatedEvent(
        UserId UserId,
        string Reason,
        DateTime DeactivatedAt
    ) : DomainEvent("User", UserId.ToString());
}

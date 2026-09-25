// ========================================
// AsanRezerve.UserManagement.Domain/Events/UserReactivatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record UserReactivatedEvent(
        UserId UserId,
        DateTime ReactivatedAt
    ) : DomainEvent("User", UserId.ToString());
}

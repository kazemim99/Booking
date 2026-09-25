// ========================================
// AsanRezerve.UserManagement.Domain/Events/UserActivatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record UserActivatedEvent(
        UserId UserId,
        Email Email,
        DateTime ActivatedAt
    ) : DomainEvent("User", UserId.ToString());
}

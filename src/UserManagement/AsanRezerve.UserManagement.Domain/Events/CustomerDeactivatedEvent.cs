// ========================================
// AsanRezerve.UserManagement.Domain/Events/CustomerDeactivatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record CustomerDeactivatedEvent(
        CustomerId CustomerId,
        UserId UserId,
        DateTime DeactivatedAt
    ) : DomainEvent("Customer", CustomerId.ToString());
}

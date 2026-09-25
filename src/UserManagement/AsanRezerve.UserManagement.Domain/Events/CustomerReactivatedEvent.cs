// ========================================
// AsanRezerve.UserManagement.Domain/Events/CustomerReactivatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record CustomerReactivatedEvent(
        CustomerId CustomerId,
        UserId UserId,
        DateTime ReactivatedAt
    ) : DomainEvent("Customer", CustomerId.ToString());
}

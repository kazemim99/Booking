// ========================================
// AsanRezerve.UserManagement.Domain/Events/CustomerProfileUpdatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record CustomerProfileUpdatedEvent(
        CustomerId CustomerId,
        UserId UserId,
        DateTime UpdatedAt
    ) : DomainEvent("Customer", CustomerId.ToString());
}

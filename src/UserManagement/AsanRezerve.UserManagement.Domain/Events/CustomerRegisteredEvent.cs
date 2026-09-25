// ========================================
// AsanRezerve.UserManagement.Domain/Events/CustomerRegisteredEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record CustomerRegisteredEvent(
        CustomerId CustomerId,
        UserId UserId,
        string FirstName,
        string LastName,
        DateTime RegisteredAt
    ) : DomainEvent("Customer", CustomerId.ToString());
}

// ========================================
// Booksy.UserManagement.Domain/Events/CustomerRegisteredEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record CustomerRegisteredEvent(
        CustomerId CustomerId,
        UserId UserId,
        string FirstName,
        string LastName,
        DateTime RegisteredAt
    ) : DomainEvent("Customer", CustomerId.ToString());
}

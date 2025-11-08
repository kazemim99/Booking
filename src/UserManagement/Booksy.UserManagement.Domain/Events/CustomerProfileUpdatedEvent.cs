// ========================================
// Booksy.UserManagement.Domain/Events/CustomerProfileUpdatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record CustomerProfileUpdatedEvent(
        CustomerId CustomerId,
        UserId UserId,
        DateTime UpdatedAt
    ) : DomainEvent("Customer", CustomerId.ToString());
}

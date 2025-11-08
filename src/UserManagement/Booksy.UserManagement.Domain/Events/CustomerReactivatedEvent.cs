// ========================================
// Booksy.UserManagement.Domain/Events/CustomerReactivatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record CustomerReactivatedEvent(
        CustomerId CustomerId,
        UserId UserId,
        DateTime ReactivatedAt
    ) : DomainEvent("Customer", CustomerId.ToString());
}

// ========================================
// Booksy.UserManagement.Domain/Events/CustomerDeactivatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record CustomerDeactivatedEvent(
        CustomerId CustomerId,
        UserId UserId,
        DateTime DeactivatedAt
    ) : DomainEvent("Customer", CustomerId.ToString());
}

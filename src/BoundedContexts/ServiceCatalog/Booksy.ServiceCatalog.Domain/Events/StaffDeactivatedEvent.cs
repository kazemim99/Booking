// ========================================
// Booksy.ServiceCatalog.Domain/Events/StaffDeactivatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    /// <summary>
    /// Domain event raised when a staff member is deactivated
    /// </summary>
    public sealed record StaffDeactivatedEvent(
        ProviderId ProviderId,
        Guid StaffId,
        string Reason,
        DateTime DeactivatedAt) : DomainEvent;
}

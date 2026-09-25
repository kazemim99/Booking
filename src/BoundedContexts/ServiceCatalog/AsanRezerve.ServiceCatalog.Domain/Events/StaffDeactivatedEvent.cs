// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/StaffDeactivatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
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

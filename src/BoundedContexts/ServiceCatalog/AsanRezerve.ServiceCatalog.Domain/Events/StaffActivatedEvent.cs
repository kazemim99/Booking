// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/StaffActivatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    /// <summary>
    /// Domain event raised when a staff member is activated/reactivated
    /// </summary>
    public sealed record StaffActivatedEvent(
        ProviderId ProviderId,
        Guid StaffId,
        DateTime ActivatedAt) : DomainEvent;
}

// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/StaffUpdatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    /// <summary>
    /// Domain event raised when a staff member is updated
    /// </summary>
    public sealed record StaffUpdatedEvent(
        ProviderId ProviderId,
        Guid StaffId,
        DateTime UpdatedAt) : DomainEvent;
}

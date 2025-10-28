// ========================================
// Booksy.ServiceCatalog.Domain/Events/StaffActivatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    /// <summary>
    /// Domain event raised when a staff member is activated/reactivated
    /// </summary>
    public sealed record StaffActivatedEvent(
        ProviderId ProviderId,
        Guid StaffId,
        DateTime ActivatedAt) : DomainEvent;
}

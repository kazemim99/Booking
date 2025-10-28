// ========================================
// Booksy.ServiceCatalog.Domain/Events/StaffUpdatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    /// <summary>
    /// Domain event raised when a staff member is updated
    /// </summary>
    public sealed record StaffUpdatedEvent(
        ProviderId ProviderId,
        Guid StaffId,
        DateTime UpdatedAt) : DomainEvent;
}

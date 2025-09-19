// ========================================
// Booksy.ServiceCatalog.Domain/Events/StaffAddedEvent.cs
// ========================================

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record StaffAddedEvent(
        ProviderId ProviderId,
        Guid StaffId,
        string StaffName,
        StaffRole Role,
        DateTime AddedAt) : DomainEvent;
}
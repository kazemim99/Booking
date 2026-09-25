// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/StaffAddedEvent.cs
// ========================================

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record StaffAddedEvent(
        ProviderId ProviderId,
        Guid StaffId,
        string StaffName,
        StaffRole Role,
        DateTime AddedAt) : DomainEvent;
}
// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/ServiceUpdatedEvent.cs
// ========================================

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record ServiceUpdatedEvent(
        ServiceId ServiceId,
        string ServiceName,
        string Description,
        DateTime UpdatedAt) : DomainEvent;
}
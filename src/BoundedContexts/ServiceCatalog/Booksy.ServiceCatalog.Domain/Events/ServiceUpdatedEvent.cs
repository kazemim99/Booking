// ========================================
// Booksy.ServiceCatalog.Domain/Events/ServiceUpdatedEvent.cs
// ========================================

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record ServiceUpdatedEvent(
        ServiceId ServiceId,
        string ServiceName,
        string Description,
        DateTime UpdatedAt) : DomainEvent;
}
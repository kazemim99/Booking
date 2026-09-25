// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/BusinessHoursUpdatedEvent.cs
// ========================================

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record BusinessHoursUpdatedEvent(
        ProviderId ProviderId,
        DateTime UpdatedAt) : DomainEvent;
}
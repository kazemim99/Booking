// ========================================
// Booksy.ServiceCatalog.Domain/Events/BusinessHoursUpdatedEvent.cs
// ========================================

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record BusinessHoursUpdatedEvent(
        ProviderId ProviderId,
        DateTime UpdatedAt) : DomainEvent;
}
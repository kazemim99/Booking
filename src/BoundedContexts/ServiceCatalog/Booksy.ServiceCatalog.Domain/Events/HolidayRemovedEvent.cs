// ========================================
// Booksy.ServiceCatalog.Domain/Events/HolidayRemovedEvent.cs
// ========================================

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record HolidayRemovedEvent(
        ProviderId ProviderId,
        Guid HolidayId,
        DateTime RemovedAt) : DomainEvent;
}

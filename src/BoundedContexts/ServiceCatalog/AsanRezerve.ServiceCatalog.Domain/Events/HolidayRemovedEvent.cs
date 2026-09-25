// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/HolidayRemovedEvent.cs
// ========================================

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record HolidayRemovedEvent(
        ProviderId ProviderId,
        Guid HolidayId,
        DateTime RemovedAt) : DomainEvent;
}

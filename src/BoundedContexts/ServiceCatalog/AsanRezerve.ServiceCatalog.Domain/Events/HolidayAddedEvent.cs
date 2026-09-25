// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/HolidayAddedEvent.cs
// ========================================

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record HolidayAddedEvent(
        ProviderId ProviderId,
        Guid HolidayId,
        DateOnly Date,
        string Reason,
        DateTime AddedAt) : DomainEvent;
}

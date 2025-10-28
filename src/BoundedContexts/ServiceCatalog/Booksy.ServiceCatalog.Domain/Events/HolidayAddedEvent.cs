// ========================================
// Booksy.ServiceCatalog.Domain/Events/HolidayAddedEvent.cs
// ========================================

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record HolidayAddedEvent(
        ProviderId ProviderId,
        Guid HolidayId,
        DateOnly Date,
        string Reason,
        DateTime AddedAt) : DomainEvent;
}

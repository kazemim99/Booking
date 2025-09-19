// ========================================
// Booksy.ServiceCatalog.Domain/Events/BusinessHoursUpdatedEvent.cs
// ========================================

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record BusinessHoursUpdatedEvent(
        ProviderId ProviderId,
        Enums.DayOfWeek DayOfWeek,
        TimeOnly OpenTime,
        TimeOnly CloseTime,
        DateTime UpdatedAt) : DomainEvent;
}
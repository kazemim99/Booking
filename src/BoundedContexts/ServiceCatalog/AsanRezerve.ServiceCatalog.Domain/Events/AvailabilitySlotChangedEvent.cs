// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/AvailabilitySlotChangedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    /// <summary>
    /// Domain event raised when an availability slot status changes
    /// Used for real-time availability updates and cache invalidation
    /// </summary>
    public sealed record AvailabilitySlotChangedEvent(
        Guid AvailabilityId,
        ProviderId ProviderId,
        DateTime Date,
        TimeOnly StartTime,
        TimeOnly EndTime,
        AvailabilityStatus PreviousStatus,
        AvailabilityStatus NewStatus,
        Guid? BookingId,
        DateTime ChangedAt) : DomainEvent;
}

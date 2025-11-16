// ========================================
// Booksy.ServiceCatalog.Domain/Events/AvailabilitySlotChangedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
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

// ========================================
// Booksy.ServiceCatalog.Domain/Events/BookingCompletedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record BookingCompletedEvent(
        BookingId BookingId,
        UserId CustomerId,
        ProviderId ProviderId,
        ServiceId ServiceId,
        Guid StaffId,
        DateTime ScheduledTime,
        DateTime CompletedAt) : DomainEvent;
}

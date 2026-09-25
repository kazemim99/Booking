// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/BookingNoShowEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record BookingNoShowEvent(
        BookingId BookingId,
        UserId CustomerId,
        ProviderId ProviderId,
        ServiceId ServiceId,
        Guid StaffId,
        DateTime ScheduledTime,
        Money ForfeitedAmount,
        DateTime MarkedAt) : DomainEvent;
}

// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/BookingConfirmedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record BookingConfirmedEvent(
        BookingId BookingId,
        UserId CustomerId,
        ProviderId ProviderId,
        ServiceId ServiceId,
        Guid StaffId,
        DateTime StartTime,
        DateTime EndTime,
        DateTime ConfirmedAt) : DomainEvent;
}

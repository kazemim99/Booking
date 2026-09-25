// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/BookingRequestedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record BookingRequestedEvent(
        BookingId BookingId,
        UserId CustomerId,
        ProviderId ProviderId,
        ServiceId ServiceId,
        Guid StaffId,
        DateTime StartTime,
        DateTime EndTime,
        Price TotalPrice,
        DateTime RequestedAt) : DomainEvent;
}

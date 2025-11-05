// ========================================
// Booksy.ServiceCatalog.Domain/Events/BookingRequestedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
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

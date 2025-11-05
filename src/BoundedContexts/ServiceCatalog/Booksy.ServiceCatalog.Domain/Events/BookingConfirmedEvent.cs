// ========================================
// Booksy.ServiceCatalog.Domain/Events/BookingConfirmedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
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

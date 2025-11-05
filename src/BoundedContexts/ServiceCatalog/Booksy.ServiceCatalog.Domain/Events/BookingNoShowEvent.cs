// ========================================
// Booksy.ServiceCatalog.Domain/Events/BookingNoShowEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
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

// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/BookingRescheduledEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record BookingRescheduledEvent(
        BookingId OldBookingId,
        BookingId NewBookingId,
        UserId CustomerId,
        ProviderId ProviderId,
        DateTime OldStartTime,
        DateTime NewStartTime,
        Guid OldStaffId,
        Guid NewStaffId,
        string? Reason,
        DateTime RescheduledAt) : DomainEvent;
}

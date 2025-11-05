// ========================================
// Booksy.ServiceCatalog.Domain/Events/BookingRescheduledEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
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

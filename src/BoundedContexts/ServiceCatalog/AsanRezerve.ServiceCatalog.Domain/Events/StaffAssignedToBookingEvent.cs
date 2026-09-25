// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/StaffAssignedToBookingEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record StaffAssignedToBookingEvent(
        BookingId BookingId,
        UserId CustomerId,
        ProviderId ProviderId,
        ServiceId ServiceId,
        Guid PreviousStaffId,
        Guid NewStaffId,
        DateTime BookingStartTime,
        DateTime AssignedAt) : DomainEvent;
}

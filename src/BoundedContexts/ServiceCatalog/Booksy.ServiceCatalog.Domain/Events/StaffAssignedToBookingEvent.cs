// ========================================
// Booksy.ServiceCatalog.Domain/Events/StaffAssignedToBookingEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
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

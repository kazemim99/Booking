// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/BookingNotesAddedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record BookingNotesAddedEvent(
        BookingId BookingId,
        UserId CustomerId,
        ProviderId ProviderId,
        string Notes,
        string AddedBy,
        bool IsStaffNote,
        DateTime AddedAt) : DomainEvent;
}

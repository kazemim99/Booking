// ========================================
// Booksy.ServiceCatalog.Domain/Events/BookingNotesAddedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
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

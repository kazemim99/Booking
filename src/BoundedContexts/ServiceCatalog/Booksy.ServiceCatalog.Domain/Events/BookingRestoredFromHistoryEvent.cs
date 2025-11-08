// ========================================
// Booksy.ServiceCatalog.Domain/Events/BookingRestoredFromHistoryEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    /// <summary>
    /// Domain event raised when a booking is restored from a historical state snapshot.
    /// </summary>
    public sealed record BookingRestoredFromHistoryEvent(
        BookingId BookingId,
        UserId CustomerId,
        ProviderId ProviderId,
        Guid StateId,
        string FromStateName,
        BookingStatus FromStatus,
        BookingStatus ToStatus,
        string? RestoredBy,
        DateTime RestoredAt) : DomainEvent;
}

// ========================================
// Booksy.ServiceCatalog.Domain/Events/BookingCancelledEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record BookingCancelledEvent(
        BookingId BookingId,
        UserId CustomerId,
        ProviderId ProviderId,
        ServiceId ServiceId,
        Guid StaffId,
        string Reason,
        bool WithoutFee,
        decimal CancellationFee,
        bool ByProvider,
        DateTime CancelledAt) : DomainEvent;
}

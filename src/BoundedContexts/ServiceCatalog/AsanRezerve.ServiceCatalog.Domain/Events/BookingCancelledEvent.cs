// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/BookingCancelledEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
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

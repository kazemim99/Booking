// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/PaymentCapturedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions.Events;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record PaymentCapturedEvent(
        PaymentId PaymentId,
        BookingId? BookingId,
        UserId CustomerId,
        ProviderId ProviderId,
        Money Amount,
        DateTime CapturedAt) : DomainEvent;
}

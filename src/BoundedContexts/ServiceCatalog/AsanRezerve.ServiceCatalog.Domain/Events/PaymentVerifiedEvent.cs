// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/PaymentVerifiedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions.Events;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    /// <summary>
    /// Domain event raised when a payment is verified by the payment gateway
    /// </summary>
    public sealed record PaymentVerifiedEvent(
        PaymentId PaymentId,
        BookingId? BookingId,
        UserId CustomerId,
        ProviderId ProviderId,
        Money Amount,
        string RefNumber,
        string? CardPan,
        DateTime VerifiedAt) : DomainEvent;
}

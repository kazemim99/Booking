// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/PaymentRequestCreatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions.Events;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    /// <summary>
    /// Domain event raised when a payment request is created with the payment gateway
    /// </summary>
    public sealed record PaymentRequestCreatedEvent(
        PaymentId PaymentId,
        BookingId? BookingId,
        UserId CustomerId,
        string Authority,
        string PaymentUrl,
        DateTime CreatedAt) : DomainEvent;
}

// ========================================
// Booksy.ServiceCatalog.Domain/Events/PaymentRequestCreatedEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
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

// ========================================
// Booksy.ServiceCatalog.Domain/Events/PaymentVerifiedEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
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

// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/PaymentFailedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions.Events;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record PaymentFailedEvent(
        PaymentId PaymentId,
        BookingId? BookingId,
        UserId CustomerId,
        string FailureReason,
        DateTime FailedAt) : DomainEvent;
}

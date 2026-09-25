// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/PaymentRefundedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions.Events;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record PaymentRefundedEvent(
        PaymentId PaymentId,
        BookingId? BookingId,
        UserId CustomerId,
        ProviderId ProviderId,
        Money RefundAmount,
        RefundReason Reason,
        DateTime RefundedAt) : DomainEvent;
}

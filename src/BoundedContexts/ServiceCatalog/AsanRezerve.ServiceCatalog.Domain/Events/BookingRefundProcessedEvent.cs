// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/BookingRefundProcessedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record BookingRefundProcessedEvent(
        BookingId BookingId,
        UserId CustomerId,
        Money RefundAmount,
        PaymentStatus PaymentStatus,
        string RefundId,
        string Reason,
        DateTime ProcessedAt) : DomainEvent;
}

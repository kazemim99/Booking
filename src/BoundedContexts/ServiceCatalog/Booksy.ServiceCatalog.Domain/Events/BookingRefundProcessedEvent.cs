// ========================================
// Booksy.ServiceCatalog.Domain/Events/BookingRefundProcessedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
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

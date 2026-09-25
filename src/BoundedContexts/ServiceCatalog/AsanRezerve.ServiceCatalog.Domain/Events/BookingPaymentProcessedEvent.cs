// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/BookingPaymentProcessedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record BookingPaymentProcessedEvent(
        BookingId BookingId,
        UserId CustomerId,
        Money Amount,
        PaymentStatus PaymentStatus,
        string PaymentIntentId,
        DateTime ProcessedAt) : DomainEvent;
}

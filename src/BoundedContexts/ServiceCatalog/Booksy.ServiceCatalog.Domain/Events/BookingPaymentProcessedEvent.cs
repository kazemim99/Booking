// ========================================
// Booksy.ServiceCatalog.Domain/Events/BookingPaymentProcessedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record BookingPaymentProcessedEvent(
        BookingId BookingId,
        UserId CustomerId,
        Money Amount,
        PaymentStatus PaymentStatus,
        string PaymentIntentId,
        DateTime ProcessedAt) : DomainEvent;
}

// ========================================
// Booksy.ServiceCatalog.Domain/Events/PaymentProcessedEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record PaymentProcessedEvent(
        PaymentId PaymentId,
        BookingId? BookingId,
        UserId CustomerId,
        ProviderId ProviderId,
        Money Amount,
        DateTime ProcessedAt) : DomainEvent;
}

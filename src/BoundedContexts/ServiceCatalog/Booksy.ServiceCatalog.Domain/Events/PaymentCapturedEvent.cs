// ========================================
// Booksy.ServiceCatalog.Domain/Events/PaymentCapturedEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record PaymentCapturedEvent(
        PaymentId PaymentId,
        BookingId? BookingId,
        UserId CustomerId,
        ProviderId ProviderId,
        Money Amount,
        DateTime CapturedAt) : DomainEvent;
}

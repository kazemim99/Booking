// ========================================
// Booksy.ServiceCatalog.Domain/Events/PaymentFailedEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record PaymentFailedEvent(
        PaymentId PaymentId,
        BookingId? BookingId,
        UserId CustomerId,
        string FailureReason,
        DateTime FailedAt) : DomainEvent;
}

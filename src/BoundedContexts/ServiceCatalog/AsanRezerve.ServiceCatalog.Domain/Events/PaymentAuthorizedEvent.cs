// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/PaymentAuthorizedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions.Events;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record PaymentAuthorizedEvent(
        PaymentId PaymentId,
        BookingId? BookingId,
        UserId CustomerId,
        Money Amount,
        DateTime AuthorizedAt) : DomainEvent;
}

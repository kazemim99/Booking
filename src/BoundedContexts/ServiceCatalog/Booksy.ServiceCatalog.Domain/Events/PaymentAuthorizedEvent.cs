// ========================================
// Booksy.ServiceCatalog.Domain/Events/PaymentAuthorizedEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record PaymentAuthorizedEvent(
        PaymentId PaymentId,
        BookingId? BookingId,
        UserId CustomerId,
        Money Amount,
        DateTime AuthorizedAt) : DomainEvent;
}

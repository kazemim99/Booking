// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/PayoutCancelledEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions.Events;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record PayoutCancelledEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        string Reason,
        DateTime CancelledAt) : DomainEvent;
}

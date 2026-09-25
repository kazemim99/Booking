// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/PayoutScheduledEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record PayoutScheduledEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        DateTime ScheduledAt) : DomainEvent;
}

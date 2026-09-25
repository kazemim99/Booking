// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/PayoutCreatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record PayoutCreatedEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        Money NetAmount,
        DateTime PeriodStart,
        DateTime PeriodEnd,
        DateTime CreatedAt) : DomainEvent;
}

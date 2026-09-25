// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/PayoutReleasedFromHoldEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record PayoutReleasedFromHoldEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        DateTime ReleasedAt) : DomainEvent;
}

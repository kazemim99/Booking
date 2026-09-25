// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/ProviderActivatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record ProviderActivatedEvent(
        ProviderId ProviderId,
        DateTime ActivatedAt) : DomainEvent;
}
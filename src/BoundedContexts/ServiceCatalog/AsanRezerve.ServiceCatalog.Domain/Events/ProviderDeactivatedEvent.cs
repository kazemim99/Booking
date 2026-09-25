// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/ProviderDeactivatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record ProviderDeactivatedEvent(
        ProviderId ProviderId,
        DateTime DeactivatedAt,
        string Reason) : DomainEvent;
}
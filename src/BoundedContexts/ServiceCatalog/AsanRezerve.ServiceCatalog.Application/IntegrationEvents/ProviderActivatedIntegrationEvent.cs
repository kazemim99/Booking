// ========================================
// AsanRezerve.ServiceCatalog.Application/IntegrationEvents/ProviderActivatedIntegrationEvent.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.Events;

namespace AsanRezerve.ServiceCatalog.Application.IntegrationEvents
{
    public sealed record ProviderActivatedIntegrationEvent(
        Guid ProviderId,
        DateTime ActivatedAt) : IntegrationEvent;
}
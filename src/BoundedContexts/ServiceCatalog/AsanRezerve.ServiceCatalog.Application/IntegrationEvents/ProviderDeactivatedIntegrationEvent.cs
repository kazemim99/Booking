// ========================================
// AsanRezerve.ServiceCatalog.Application/IntegrationEvents/ProviderDeactivatedIntegrationEvent.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.Events;

namespace AsanRezerve.ServiceCatalog.Application.IntegrationEvents
{
    public sealed record ProviderDeactivatedIntegrationEvent(
        Guid ProviderId,
        DateTime DeactivatedAt,
        string Reason) : IntegrationEvent;
}
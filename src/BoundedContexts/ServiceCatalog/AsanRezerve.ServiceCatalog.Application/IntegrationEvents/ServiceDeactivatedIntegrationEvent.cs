// ========================================
// AsanRezerve.ServiceCatalog.Application/IntegrationEvents/ServiceDeactivatedIntegrationEvent.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.Events;

namespace AsanRezerve.ServiceCatalog.Application.IntegrationEvents
{
    public sealed record ServiceDeactivatedIntegrationEvent(
        Guid ServiceId,
        Guid ProviderId,
        string ServiceName,
        DateTime DeactivatedAt,
        string Reason) : IntegrationEvent;
}
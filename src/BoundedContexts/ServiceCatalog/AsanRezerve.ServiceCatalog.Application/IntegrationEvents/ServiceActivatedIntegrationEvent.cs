// ========================================
// AsanRezerve.ServiceCatalog.Application/IntegrationEvents/ServiceActivatedIntegrationEvent.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.Events;

namespace AsanRezerve.ServiceCatalog.Application.IntegrationEvents
{
    public sealed record ServiceActivatedIntegrationEvent(
        Guid ServiceId,
        Guid ProviderId,
        string ServiceName,
        DateTime ActivatedAt) : IntegrationEvent;
}
// ========================================
// Booksy.ServiceCatalog.Application/IntegrationEvents/ServiceActivatedIntegrationEvent.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;

namespace Booksy.ServiceCatalog.Application.IntegrationEvents
{
    public sealed record ServiceActivatedIntegrationEvent(
        Guid ServiceId,
        Guid ProviderId,
        string ServiceName,
        DateTime ActivatedAt) : IntegrationEvent;
}
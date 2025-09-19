// ========================================
// Booksy.ServiceCatalog.Application/IntegrationEvents/ServiceDeactivatedIntegrationEvent.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;

namespace Booksy.ServiceCatalog.Application.IntegrationEvents
{
    public sealed record ServiceDeactivatedIntegrationEvent(
        Guid ServiceId,
        Guid ProviderId,
        string ServiceName,
        DateTime DeactivatedAt,
        string Reason) : IntegrationEvent;
}
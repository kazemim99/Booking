// ========================================
// Booksy.ServiceCatalog.Application/IntegrationEvents/ProviderDeactivatedIntegrationEvent.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;

namespace Booksy.ServiceCatalog.Application.IntegrationEvents
{
    public sealed record ProviderDeactivatedIntegrationEvent(
        Guid ProviderId,
        DateTime DeactivatedAt,
        string Reason) : IntegrationEvent;
}
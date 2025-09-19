// ========================================
// Booksy.ServiceCatalog.Application/IntegrationEvents/ProviderActivatedIntegrationEvent.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;

namespace Booksy.ServiceCatalog.Application.IntegrationEvents
{
    public sealed record ProviderActivatedIntegrationEvent(
        Guid ProviderId,
        DateTime ActivatedAt) : IntegrationEvent;
}
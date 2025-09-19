// ========================================
// Booksy.ServiceCatalog.Application/IntegrationEvents/ProviderRegisteredIntegrationEvent.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.IntegrationEvents
{
    public sealed record ProviderRegisteredIntegrationEvent(
        Guid ProviderId,
        Guid OwnerId,
        string BusinessName,
        ProviderType ProviderType,
        DateTime RegisteredAt) : IntegrationEvent;
}
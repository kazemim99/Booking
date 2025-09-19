// ========================================
// Booksy.ServiceCatalog.Application/IntegrationEvents/ServiceCreatedIntegrationEvent.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;

namespace Booksy.ServiceCatalog.Application.IntegrationEvents
{
    public sealed record ServiceCreatedIntegrationEvent(
        Guid ServiceId,
        Guid ProviderId,
        string ServiceName,
        string Category,
        decimal BasePrice,
        string Currency,
        int DurationMinutes,
        DateTime CreatedAt) : IntegrationEvent;
}
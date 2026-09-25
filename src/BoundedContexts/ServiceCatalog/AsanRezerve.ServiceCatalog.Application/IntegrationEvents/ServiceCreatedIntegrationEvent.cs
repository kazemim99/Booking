// ========================================
// AsanRezerve.ServiceCatalog.Application/IntegrationEvents/ServiceCreatedIntegrationEvent.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.Events;

namespace AsanRezerve.ServiceCatalog.Application.IntegrationEvents
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
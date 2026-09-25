// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/ServiceCreatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record ServiceCreatedEvent(
        ServiceId ServiceId,
        ProviderId ProviderId,
        string ServiceName,
        ServiceCategory Category,
        Price BasePrice,
        Duration Duration,
        DateTime CreatedAt) : DomainEvent;
}
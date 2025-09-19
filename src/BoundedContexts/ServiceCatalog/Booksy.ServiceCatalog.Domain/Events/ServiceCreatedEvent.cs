// ========================================
// Booksy.ServiceCatalog.Domain/Events/ServiceCreatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
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
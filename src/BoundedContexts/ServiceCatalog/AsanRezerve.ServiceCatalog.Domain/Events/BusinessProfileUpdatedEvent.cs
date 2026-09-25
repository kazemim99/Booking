// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/BusinessProfileUpdatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record BusinessProfileUpdatedEvent(
        ProviderId ProviderId,
        string BusinessName,
        string Description,
        DateTime UpdatedAt) : DomainEvent;
}
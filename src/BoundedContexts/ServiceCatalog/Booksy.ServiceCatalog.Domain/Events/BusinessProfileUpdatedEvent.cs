// ========================================
// Booksy.ServiceCatalog.Domain/Events/BusinessProfileUpdatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record BusinessProfileUpdatedEvent(
        ProviderId ProviderId,
        string BusinessName,
        string Description,
        DateTime UpdatedAt) : DomainEvent;
}
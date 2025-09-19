// ========================================
// Booksy.ServiceCatalog.Domain/Events/ProviderDeactivatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record ProviderDeactivatedEvent(
        ProviderId ProviderId,
        DateTime DeactivatedAt,
        string Reason) : DomainEvent;
}
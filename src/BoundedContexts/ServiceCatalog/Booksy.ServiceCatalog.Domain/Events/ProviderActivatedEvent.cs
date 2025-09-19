// ========================================
// Booksy.ServiceCatalog.Domain/Events/ProviderActivatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record ProviderActivatedEvent(
        ProviderId ProviderId,
        DateTime ActivatedAt) : DomainEvent;
}
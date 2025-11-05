// ========================================
// Booksy.ServiceCatalog.Domain/Events/PayoutScheduledEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record PayoutScheduledEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        DateTime ScheduledAt) : IDomainEvent;
}

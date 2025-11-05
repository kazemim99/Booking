// ========================================
// Booksy.ServiceCatalog.Domain/Events/PayoutCreatedEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record PayoutCreatedEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        Money NetAmount,
        DateTime PeriodStart,
        DateTime PeriodEnd,
        DateTime CreatedAt) : IDomainEvent;
}

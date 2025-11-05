// ========================================
// Booksy.ServiceCatalog.Domain/Events/PayoutReleasedFromHoldEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record PayoutReleasedFromHoldEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        DateTime ReleasedAt) : IDomainEvent;
}

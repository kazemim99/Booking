// ========================================
// Booksy.ServiceCatalog.Domain/Events/PayoutCompletedEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record PayoutCompletedEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        Money Amount,
        DateTime PaidAt) : DomainEvent;
}

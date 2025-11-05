// ========================================
// Booksy.ServiceCatalog.Domain/Events/PayoutCancelledEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record PayoutCancelledEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        string Reason,
        DateTime CancelledAt) : DomainEvent;
}

// ========================================
// Booksy.ServiceCatalog.Domain/Events/PayoutFailedEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record PayoutFailedEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        string FailureReason,
        DateTime FailedAt) : DomainEvent;
}

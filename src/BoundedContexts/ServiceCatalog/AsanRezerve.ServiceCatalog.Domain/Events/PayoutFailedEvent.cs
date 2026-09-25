// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/PayoutFailedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions.Events;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record PayoutFailedEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        string FailureReason,
        DateTime FailedAt) : DomainEvent;
}

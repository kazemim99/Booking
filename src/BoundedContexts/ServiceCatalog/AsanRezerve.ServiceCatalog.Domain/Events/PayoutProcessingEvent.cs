// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/PayoutProcessingEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record PayoutProcessingEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        string ExternalPayoutId,
        DateTime ProcessingAt) : DomainEvent;
}

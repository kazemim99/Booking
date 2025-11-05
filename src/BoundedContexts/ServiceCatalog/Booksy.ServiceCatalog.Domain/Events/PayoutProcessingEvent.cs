// ========================================
// Booksy.ServiceCatalog.Domain/Events/PayoutProcessingEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record PayoutProcessingEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        string ExternalPayoutId,
        DateTime ProcessingAt) : IDomainEvent;
}

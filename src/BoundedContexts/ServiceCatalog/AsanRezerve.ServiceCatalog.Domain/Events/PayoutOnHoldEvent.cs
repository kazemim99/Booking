// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/PayoutOnHoldEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record PayoutOnHoldEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        string Reason,
        DateTime HeldAt) : DomainEvent;
}

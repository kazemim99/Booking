// ========================================
// Booksy.ServiceCatalog.Domain/Events/PayoutOnHoldEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record PayoutOnHoldEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        string Reason,
        DateTime HeldAt) : DomainEvent;
}

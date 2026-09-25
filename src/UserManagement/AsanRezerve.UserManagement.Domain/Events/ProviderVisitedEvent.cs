// ========================================
// AsanRezerve.UserManagement.Domain/Events/ProviderVisitedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    /// <summary>
    /// Domain event raised when a customer visits/views a provider profile
    /// </summary>
    public sealed record ProviderVisitedEvent(
        CustomerId CustomerId,
        Guid ProviderId,
        DateTime VisitedAt,
        string? ViewSource
    ) : DomainEvent("Customer", CustomerId.ToString());
}

// ========================================
// Booksy.UserManagement.Domain/Events/ProviderVisitedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
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

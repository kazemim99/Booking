// ========================================
// Booksy.ServiceCatalog.Domain/Events/ProviderDraftCreatedEvent.cs
// ========================================

namespace Booksy.ServiceCatalog.Domain.Events
{
    /// <summary>
    /// Domain event raised when a provider draft is created (Step 3 completion)
    /// Contains owner information to update User profile in UserManagement context
    /// </summary>
    public sealed record ProviderDraftCreatedEvent(
        ProviderId ProviderId,
        UserId OwnerId,
        string OwnerFirstName,
        string OwnerLastName,
        string BusinessName,
        DateTime CreatedAt) : DomainEvent;
}

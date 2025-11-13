// ========================================
// Booksy.ServiceCatalog.Application/IntegrationEvents/ProviderDraftCreatedIntegrationEvent.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;

namespace Booksy.ServiceCatalog.Application.IntegrationEvents
{
    /// <summary>
    /// Integration event raised when a provider draft is created (Step 3 completion)
    /// Published to: booksy.servicecatalog.providerdraftcreated
    /// Consumed by: UserManagement context to update User.Profile with owner's name
    /// </summary>
    public sealed record ProviderDraftCreatedIntegrationEvent(
        Guid ProviderId,
        Guid OwnerId,
        string OwnerFirstName,
        string OwnerLastName,
        string BusinessName,
        DateTime CreatedAt) : IntegrationEvent
    {
        public ProviderDraftCreatedIntegrationEvent() : this(Guid.Empty, Guid.Empty, string.Empty, string.Empty, string.Empty, DateTime.UtcNow)
        {
            // Required for CAP deserialization
        }
    }
}

// ========================================
// Booksy.ServiceCatalog.Application/IntegrationEvents/ProviderRegisteredIntegrationEvent.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;

namespace Booksy.ServiceCatalog.Application.IntegrationEvents
{
    /// <summary>
    /// Integration event raised when a provider completes registration
    /// Published to: booksy.servicecatalog.providerregistered
    /// Consumed by: UserManagement context
    /// </summary>
    public sealed record ProviderRegisteredIntegrationEvent(
        Guid ProviderId,
        Guid OwnerId,
        string BusinessName,
        string ProviderType, // String instead of enum for cross-context compatibility
        DateTime RegisteredAt) : IntegrationEvent
    {
        public ProviderRegisteredIntegrationEvent() : this(Guid.Empty, Guid.Empty, string.Empty, string.Empty, DateTime.UtcNow)
        {
            // Required for CAP deserialization
        }
    }
}
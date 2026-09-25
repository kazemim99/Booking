// ========================================
// Event Handlers - Domain Events
// ========================================

// AsanRezerve.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
using AsanRezerve.Core.Application.Abstractions.Events;
using AsanRezerve.UserManagement.Application.Abstractions.Events;

namespace AsanRezerve.UserManagement.Application.EventHandlers
{
    public sealed class CreateProviderProfileEventHandler : IIntegrationEventHandler<UserCreatedIntegrationEvent>
    {
        private readonly ILogger<CreateProviderProfileEventHandler> _logger;

        public CreateProviderProfileEventHandler(ILogger<CreateProviderProfileEventHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(UserCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Creating provider profile for UserId: {UserId}",
                integrationEvent.UserId);

            // This would typically call a service in the Provider/ServiceCatalog bounded context
            // to create a provider profile when a provider user is created

            await Task.CompletedTask;
        }
    }
}


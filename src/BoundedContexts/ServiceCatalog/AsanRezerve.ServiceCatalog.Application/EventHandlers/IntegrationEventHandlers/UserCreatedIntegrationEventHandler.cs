//// ========================================
//// AsanRezerve.ServiceCatalog.Application/EventHandlers/IntegrationEventHandlers/UserCreatedIntegrationEventHandler.cs
//// ========================================
//using AsanRezerve.Core.Application.Abstractions.Events;
//using AsanRezerve.UserManagement.Application.IntegrationEvents;
//using Microsoft.Extensions.Logging;

//namespace AsanRezerve.ServiceCatalog.Application.EventHandlers.IntegrationEventHandlers
//{
//    public sealed class UserCreatedIntegrationEventHandler : IIntegrationEventHandler<UserCreatedIntegrationEvent>
//    {
//        private readonly ILogger<UserCreatedIntegrationEventHandler> _logger;

//        public UserCreatedIntegrationEventHandler(ILogger<UserCreatedIntegrationEventHandler> logger)
//        {
//            _logger = logger;
//        }

//        public async Task HandleAsync(UserCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
//        {
//            _logger.LogInformation(
//                "User created integration event received for UserId: {UserId}, UserType: {UserType}",
//                integrationEvent.UserId,
//                integrationEvent.UserType);

//            // If user type is Provider, we might want to send them onboarding information
//            if (integrationEvent.UserType == "Provider")
//            {
//                _logger.LogInformation("Provider user created, consider sending onboarding information");
//                // Here you could trigger email sequences, create initial provider setup, etc.
//            }

//            await Task.CompletedTask;
//        }
//    }
//}
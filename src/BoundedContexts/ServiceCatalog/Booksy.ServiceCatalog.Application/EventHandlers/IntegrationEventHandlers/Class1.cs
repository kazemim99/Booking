//// ========================================
//// Booksy.ServiceCatalog.Application/EventHandlers/IntegrationEventHandlers/UserDeactivatedIntegrationEventHandler.cs
//// ========================================
//using Booksy.Core.Application.Abstractions.Events;
//using Booksy.ServiceCatalog.Domain.Repositories;
//using Microsoft.Extensions.Logging;

//namespace Booksy.ServiceCatalog.Application.EventHandlers.IntegrationEventHandlers
//{
//    public sealed class UserDeactivatedIntegrationEventHandler : IIntegrationEventHandler<UserDeactivatedIntegrationEvent>
//    {
//        private readonly IProviderReadRepository _providerReadRepository;
//        private readonly IProviderWriteRepository _providerWriteRepository;
//        private readonly ILogger<UserDeactivatedIntegrationEventHandler> _logger;

//        public UserDeactivatedIntegrationEventHandler(
//            IProviderReadRepository providerReadRepository,
//            IProviderWriteRepository providerWriteRepository,
//            ILogger<UserDeactivatedIntegrationEventHandler> logger)
//        {
//            _providerReadRepository = providerReadRepository;
//            _providerWriteRepository = providerWriteRepository;
//            _logger = logger;
//        }

//        public async Task HandleAsync(UserDeactivatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
//        {
//            _logger.LogInformation(
//                "Processing user deactivation for UserId: {UserId}",
//                integrationEvent.UserId);

//            // Find provider owned by this user
//            var ownerId = UserId.From(integrationEvent.UserId);
//            var provider = await _providerReadRepository.GetByOwnerIdAsync(ownerId, cancellationToken);

//            if (provider != null)
//            {
//                // Deactivate the provider when user is deactivated
//                provider.Deactivate("Owner account deactivated");

//                await _providerWriteRepository.UpdateProviderAsync(provider, cancellationToken);

//                _logger.LogInformation(
//                    "Provider deactivated due to user deactivation: ProviderId={ProviderId}, UserId={UserId}",
//                    provider.Id,
//                    integrationEvent.UserId);
//            }
//            else
//            {
//                _logger.LogInformation("No provider found for deactivated user: {UserId}", integrationEvent.UserId);
//            }
//        }
//    }
//}
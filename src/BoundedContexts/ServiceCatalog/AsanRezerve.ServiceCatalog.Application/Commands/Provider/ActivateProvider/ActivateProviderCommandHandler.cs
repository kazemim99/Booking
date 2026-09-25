// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/ActivateProvider/ActivateProviderCommandHandler.cs
// ========================================
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.ActivateProvider
{
    public sealed class ActivateProviderCommandHandler : ICommandHandler<ActivateProviderCommand, ActivateProviderResult>
    {
        private readonly IProviderWriteRepository _providerWriteRepository;
        private readonly IProviderReadRepository _providerReadRepository;
        private readonly INotificationRaiser _notifications;
        private readonly ILogger<ActivateProviderCommandHandler> _logger;

        public ActivateProviderCommandHandler(
            IProviderWriteRepository providerWriteRepository,
            IProviderReadRepository providerReadRepository,
            INotificationRaiser notifications,
            ILogger<ActivateProviderCommandHandler> logger)
        {
            _providerWriteRepository = providerWriteRepository;
            _providerReadRepository = providerReadRepository;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task<ActivateProviderResult> Handle(
            ActivateProviderCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Activating provider: {ProviderId}", request.ProviderId);

            var providerId = ProviderId.From(request.ProviderId);
            var provider = await _providerReadRepository.GetByIdAsync(providerId, cancellationToken);

            if (provider == null)
                throw new NotFoundException("Provider not found");

            provider.Activate();

            await _providerWriteRepository.UpdateProviderAsync(provider, cancellationToken);

            // Activation decides whether the salon can trade at all, so it is non-suppressible in the
            // catalogue. Addressed to the owner as a person, never to the provider id.
            await _notifications.RaiseAsync(
                Domain.Enums.NotificationEventCode.ProviderActivated,
                provider.OwnerId.Value,
                dedupKey: provider.Id.Value,
                parameters: new Dictionary<string, string>
                {
                    [NotificationParameter.BusinessName] = provider.Profile.BusinessName,
                },
                subjectType: "Provider",
                subjectId: provider.Id.Value,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Provider activated successfully: {ProviderId}", provider.Id);

            return new ActivateProviderResult(
                ProviderId: provider.Id.Value,
                BusinessName: provider.Profile.BusinessName,
                ActivatedAt: provider.ActivatedAt!.Value);
        }
    }
}
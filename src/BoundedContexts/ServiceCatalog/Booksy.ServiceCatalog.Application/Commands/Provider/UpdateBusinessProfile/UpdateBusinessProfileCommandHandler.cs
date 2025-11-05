using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessProfile
{
    public sealed class UpdateBusinessProfileCommandHandler : ICommandHandler<UpdateBusinessProfileCommand, UpdateBusinessProfileResult>
    {
        private readonly IProviderWriteRepository _providerWriteRepository;
        private readonly ILogger<UpdateBusinessProfileCommandHandler> _logger;

        public UpdateBusinessProfileCommandHandler(
            IProviderWriteRepository providerWriteRepository,
            ILogger<UpdateBusinessProfileCommandHandler> logger)
        {
            _providerWriteRepository = providerWriteRepository;
            _logger = logger;
        }

        public async Task<UpdateBusinessProfileResult> Handle(
            UpdateBusinessProfileCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating business profile for provider: {ProviderId}", request.ProviderId);

            var providerId = ProviderId.From(request.ProviderId);
            var provider = await _providerWriteRepository.GetByIdAsync(providerId, cancellationToken);

            if (provider == null)
                throw new InvalidProviderException("Provider not found");

            provider.UpdateBusinessProfile(request.BusinessName, request.Description, provider.Profile.ProfileImageUrl);

            provider.Profile.UpdateLogo(request.LogoUrl);


            await _providerWriteRepository.UpdateProviderAsync(provider, cancellationToken);

            _logger.LogInformation("Business profile updated for provider: {ProviderId}", provider.Id);

            return new UpdateBusinessProfileResult(
                ProviderId: provider.Id.Value,
                BusinessName: provider.Profile.BusinessName,
                Description: provider.Profile.BusinessDescription,
                UpdatedAt: DateTime.UtcNow);
        }
    }
}
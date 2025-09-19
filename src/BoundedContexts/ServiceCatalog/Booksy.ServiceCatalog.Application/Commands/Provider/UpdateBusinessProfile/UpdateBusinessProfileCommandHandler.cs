using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessProfile
{
    public sealed class UpdateBusinessProfileCommandHandler : ICommandHandler<UpdateBusinessProfileCommand, UpdateBusinessProfileResult>
    {
        private readonly IProviderWriteRepository _providerWriteRepository;
        private readonly IProviderReadRepository _providerReadRepository;
        private readonly ILogger<UpdateBusinessProfileCommandHandler> _logger;

        public UpdateBusinessProfileCommandHandler(
            IProviderWriteRepository providerWriteRepository,
            IProviderReadRepository providerReadRepository,
            ILogger<UpdateBusinessProfileCommandHandler> logger)
        {
            _providerWriteRepository = providerWriteRepository;
            _providerReadRepository = providerReadRepository;
            _logger = logger;
        }

        public async Task<UpdateBusinessProfileResult> Handle(
            UpdateBusinessProfileCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating business profile for provider: {ProviderId}", request.ProviderId);

            var providerId = ProviderId.From(request.ProviderId);
            var provider = await _providerReadRepository.GetByIdAsync(providerId, cancellationToken);

            if (provider == null)
                throw new InvalidProviderException("Provider not found");

            provider.UpdateBusinessProfile(request.BusinessName, request.Description, request.Website);

            if (!string.IsNullOrEmpty(request.LogoUrl))
            {
                provider.Profile.UpdateLogo(request.LogoUrl);
            }

            if (request.Tags?.Any() == true)
            {
                // Clear existing tags and add new ones
                provider.Profile.Tags.Clear();
                foreach (var tag in request.Tags)
                {
                    provider.Profile.AddTag(tag);
                }
            }

            if (request.SocialMedia?.Any() == true)
            {
                foreach (var social in request.SocialMedia)
                {
                    provider.Profile.AddSocialMedia(social.Key, social.Value);
                }
            }

            await _providerWriteRepository.UpdateProviderAsync(provider, cancellationToken);

            _logger.LogInformation("Business profile updated for provider: {ProviderId}", provider.Id);

            return new UpdateBusinessProfileResult(
                ProviderId: provider.Id.Value,
                BusinessName: provider.Profile.BusinessName,
                Description: provider.Profile.Description,
                UpdatedAt: DateTime.UtcNow);
        }
    }
}
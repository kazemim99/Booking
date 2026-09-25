using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.UpdateBusinessProfile
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

            // Partial-update semantics: Description and LogoUrl are optional on the request, so omitting
            // them must leave the stored values alone rather than erase them.
            //
            // Passing a null Description straight through set BusinessDescription = null and hit
            // "23502: null value in column BusinessDescription violates not-null constraint" — a 500 and
            // a failed save for a perfectly valid partial update. UpdateLogo had the same shape: a PUT
            // that omitted LogoUrl wiped the provider's existing logo.
            provider.UpdateBusinessProfile(
                request.BusinessName,
                request.Description ?? provider.Profile.BusinessDescription,
                provider.Profile.ProfileImageUrl);

            if (!string.IsNullOrWhiteSpace(request.LogoUrl))
            {
                provider.Profile.UpdateLogo(request.LogoUrl);
            }


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
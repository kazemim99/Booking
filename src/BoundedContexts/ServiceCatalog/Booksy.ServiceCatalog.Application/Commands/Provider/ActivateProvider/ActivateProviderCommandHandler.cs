// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/ActivateProvider/ActivateProviderCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.ActivateProvider
{
    public sealed class ActivateProviderCommandHandler : ICommandHandler<ActivateProviderCommand, ActivateProviderResult>
    {
        private readonly IProviderWriteRepository _providerWriteRepository;
        private readonly IProviderReadRepository _providerReadRepository;
        private readonly ILogger<ActivateProviderCommandHandler> _logger;

        public ActivateProviderCommandHandler(
            IProviderWriteRepository providerWriteRepository,
            IProviderReadRepository providerReadRepository,
            ILogger<ActivateProviderCommandHandler> logger)
        {
            _providerWriteRepository = providerWriteRepository;
            _providerReadRepository = providerReadRepository;
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

            _logger.LogInformation("Provider activated successfully: {ProviderId}", provider.Id);

            return new ActivateProviderResult(
                ProviderId: provider.Id.Value,
                BusinessName: provider.Profile.BusinessName,
                ActivatedAt: provider.ActivatedAt!.Value);
        }
    }
}
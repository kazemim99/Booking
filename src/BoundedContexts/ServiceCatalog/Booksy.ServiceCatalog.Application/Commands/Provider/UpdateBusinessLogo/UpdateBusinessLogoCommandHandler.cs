using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessLogo;

public sealed class UpdateBusinessLogoCommandHandler : ICommandHandler<UpdateBusinessLogoCommand, UpdateBusinessLogoResult>
{
    private readonly IProviderWriteRepository _providerWriteRepository;
    private readonly IProviderReadRepository _providerReadRepository;
    private readonly ILogger<UpdateBusinessLogoCommandHandler> _logger;

    public UpdateBusinessLogoCommandHandler(
        IProviderWriteRepository providerWriteRepository,
        IProviderReadRepository providerReadRepository,
        ILogger<UpdateBusinessLogoCommandHandler> logger)
    {
        _providerWriteRepository = providerWriteRepository;
        _providerReadRepository = providerReadRepository;
        _logger = logger;
    }

    public async Task<UpdateBusinessLogoResult> Handle(
        UpdateBusinessLogoCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating business logo for provider {ProviderId}",
            request.ProviderId);

        // Get provider
        var providerId = Domain.ValueObjects.ProviderId.From(request.ProviderId);
        var provider = await _providerReadRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
        {
            throw new KeyNotFoundException($"Provider with ID {request.ProviderId} not found");
        }

        // Update logo
        provider.Profile.UpdateLogo(request.LogoUrl);

        // Save changes
        await _providerWriteRepository.SaveProviderAsync(provider, cancellationToken);

        _logger.LogInformation(
            "Business logo updated successfully for provider {ProviderId}",
            request.ProviderId);

        return new UpdateBusinessLogoResult(true, "Business logo updated successfully");
    }
}

using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateLocation;

public sealed class UpdateLocationCommandHandler : ICommandHandler<UpdateLocationCommand, UpdateLocationResult>
{
    private readonly IProviderWriteRepository _providerWriteRepository;
    private readonly IProviderReadRepository _providerReadRepository;
    private readonly ILogger<UpdateLocationCommandHandler> _logger;

    public UpdateLocationCommandHandler(
        IProviderWriteRepository providerWriteRepository,
        IProviderReadRepository providerReadRepository,
        ILogger<UpdateLocationCommandHandler> logger)
    {
        _providerWriteRepository = providerWriteRepository;
        _providerReadRepository = providerReadRepository;
        _logger = logger;
    }

    public async Task<UpdateLocationResult> Handle(
        UpdateLocationCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating location for provider {ProviderId}",
            request.ProviderId);

        // Validate request
        ValidateRequest(request);

        // Get provider
        var providerId = Domain.ValueObjects.ProviderId.From(request.ProviderId);
        var provider = await _providerReadRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
        {
            throw new KeyNotFoundException($"Provider with ID {request.ProviderId} not found");
        }

        // Create and update business address
        var address = BusinessAddress.Create(
            request.FormattedAddress,
            request.AddressLine1 ?? request.FormattedAddress, // Use AddressLine1 or fallback to FormattedAddress
            request.City ?? "",
            "", // State (empty)
            request.PostalCode ?? "",
            request.Country,
            request.ProvinceId,
            request.CityId,
            request.Latitude,
            request.Longitude);

        provider.UpdateAddress(address);

        // Save changes
        await _providerWriteRepository.SaveProviderAsync(provider, cancellationToken);

        _logger.LogInformation(
            "Location updated successfully for provider {ProviderId}",
            request.ProviderId);

        return new UpdateLocationResult(
            provider.Id.Value,
            request.FormattedAddress,
            request.AddressLine1,
            request.City,
            request.PostalCode,
            request.Country,
            request.ProvinceId,
            request.CityId,
            request.Latitude,
            request.Longitude,
            DateTime.UtcNow);
    }

    private void ValidateRequest(UpdateLocationCommand request)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.FormattedAddress))
        {
            errors["formattedAddress"] = new List<string> { "Formatted address is required" };
        }

        if (request.Latitude < -90 || request.Latitude > 90)
        {
            errors["latitude"] = new List<string> { "Latitude must be between -90 and 90" };
        }

        if (request.Longitude < -180 || request.Longitude > 180)
        {
            errors["longitude"] = new List<string> { "Longitude must be between -180 and 180" };
        }

        if (errors.Any())
        {
            throw new ValidationException(errors);
        }
    }
}

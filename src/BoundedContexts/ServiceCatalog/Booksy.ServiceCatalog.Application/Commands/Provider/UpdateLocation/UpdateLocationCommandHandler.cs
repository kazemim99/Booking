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
        var providerId = Domain.ValueObjects.ProviderId.Create(request.ProviderId);
        var provider = await _providerReadRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
        {
            throw new KeyNotFoundException($"Provider with ID {request.ProviderId} not found");
        }

        // Build full street address
        var fullStreet = string.IsNullOrWhiteSpace(request.AddressLine2)
            ? request.AddressLine1
            : $"{request.AddressLine1}, {request.AddressLine2}";

        // Create and update business address
        var address = BusinessAddress.Create(
            fullStreet,
            request.City,
            request.State ?? "",
            request.PostalCode,
            request.Country,
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
            request.AddressLine1,
            request.City,
            request.PostalCode,
            request.Latitude,
            request.Longitude,
            DateTime.UtcNow);
    }

    private void ValidateRequest(UpdateLocationCommand request)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.AddressLine1))
        {
            errors["addressLine1"] = new List<string> { "Address line 1 is required" };
        }

        if (string.IsNullOrWhiteSpace(request.City))
        {
            errors["city"] = new List<string> { "City is required" };
        }

        if (string.IsNullOrWhiteSpace(request.PostalCode))
        {
            errors["postalCode"] = new List<string> { "Postal code is required" };
        }

        if (request.Latitude.HasValue && (request.Latitude < -90 || request.Latitude > 90))
        {
            errors["latitude"] = new List<string> { "Latitude must be between -90 and 90" };
        }

        if (request.Longitude.HasValue && (request.Longitude < -180 || request.Longitude > 180))
        {
            errors["longitude"] = new List<string> { "Longitude must be between -180 and 180" };
        }

        if (errors.Any())
        {
            throw new ValidationException(errors);
        }
    }
}

using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Service.AddProviderService;

public sealed class AddProviderServiceCommandHandler : ICommandHandler<AddProviderServiceCommand, AddProviderServiceResult>
{
    private readonly IServiceWriteRepository _serviceWriteRepository;
    private readonly IServiceReadRepository _serviceReadRepository;
    private readonly IProviderReadRepository _providerReadRepository;
    private readonly ILogger<AddProviderServiceCommandHandler> _logger;

    public AddProviderServiceCommandHandler(
        IServiceWriteRepository serviceWriteRepository,
        IServiceReadRepository serviceReadRepository,
        IProviderReadRepository providerReadRepository,
        ILogger<AddProviderServiceCommandHandler> logger)
    {
        _serviceWriteRepository = serviceWriteRepository;
        _serviceReadRepository = serviceReadRepository;
        _providerReadRepository = providerReadRepository;
        _logger = logger;
    }

    public async Task<AddProviderServiceResult> Handle(
        AddProviderServiceCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Adding service '{ServiceName}' for provider {ProviderId}",
            request.ServiceName,
            request.ProviderId);

        // Validate request
        await ValidateRequestAsync(request, cancellationToken);

        // Check if provider exists
        var providerId = ProviderId.Create(request.ProviderId);
        var provider = await _providerReadRepository.GetByIdAsync(providerId, cancellationToken);
        if (provider == null)
        {
            throw new KeyNotFoundException($"Provider with ID {request.ProviderId} not found");
        }

        // Check for duplicate service name for this provider
        var isDuplicate = await _serviceReadRepository.ExistsWithNameForProviderAsync(
            providerId,
            request.ServiceName,
            null,
            cancellationToken);

        if (isDuplicate)
        {
            throw new ValidationException("serviceName", $"A service with name '{request.ServiceName}' already exists for this provider");
        }

        // Create service
        var totalMinutes = (request.DurationHours * 60) + request.DurationMinutes;
        var duration = Duration.FromMinutes(totalMinutes);
        var price = Price.Create(request.Price, request.Currency);

        var serviceCategory = ParseCategory(request.Category);
        var serviceType = request.IsMobileService ? ServiceType.OnDemand : ServiceType.Standard;

        var service = Domain.Aggregates.Service.Create(
            providerId,
            request.ServiceName,
            request.Description ?? request.ServiceName,
            serviceCategory,
            serviceType,
            price,
            duration);

        // Save service
        await _serviceWriteRepository.SaveServiceAsync(service, cancellationToken);

        _logger.LogInformation(
            "Service {ServiceId} added successfully for provider {ProviderId}",
            service.Id,
            request.ProviderId);

        return new AddProviderServiceResult(
            service.Id.Value,
            provider.Id.Value,
            service.Name,
            service.BasePrice.Amount,
            service.BasePrice.Currency,
            totalMinutes,
            DateTime.UtcNow);
    }

    private async Task ValidateRequestAsync(AddProviderServiceCommand request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.ServiceName))
        {
            errors["serviceName"] = new List<string> { "Service name is required" };
        }

        if (request.ServiceName?.Length > 200)
        {
            errors["serviceName"] = new List<string> { "Service name cannot exceed 200 characters" };
        }

        var totalMinutes = (request.DurationHours * 60) + request.DurationMinutes;
        if (totalMinutes <= 0)
        {
            errors["duration"] = new List<string> { "Service duration must be greater than 0" };
        }

        if (totalMinutes > 1440) // 24 hours
        {
            errors["duration"] = new List<string> { "Service duration cannot exceed 24 hours (1440 minutes)" };
        }

        if (request.Price < 0)
        {
            errors["price"] = new List<string> { "Service price cannot be negative" };
        }

        if (string.IsNullOrWhiteSpace(request.Currency))
        {
            errors["currency"] = new List<string> { "Currency is required" };
        }

        if (errors.Any())
        {
            throw new ValidationException(errors);
        }
    }

    private ServiceCategory ParseCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return ServiceCategory.Beauty; // Default
        }

        return category.ToLowerInvariant() switch
        {
            "beauty" or "hair" or "nails" => ServiceCategory.Beauty,
            "fitness" => ServiceCategory.Fitness,
            "health" or "medical" or "dental" or "therapy" => ServiceCategory.Health,
            "education" or "training" => ServiceCategory.Education,
            "professional" or "consultation" => ServiceCategory.Professional,
            "home" => ServiceCategory.Home,
            "automotive" => ServiceCategory.Automotive,
            "pet" => ServiceCategory.Pet,
            _ => ServiceCategory.Beauty
        };
    }
}

using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Service.UpdateProviderService;

public sealed class UpdateProviderServiceCommandHandler : ICommandHandler<UpdateProviderServiceCommand, UpdateProviderServiceResult>
{
    private readonly IServiceWriteRepository _serviceWriteRepository;
    private readonly IServiceReadRepository _serviceReadRepository;
    private readonly ILogger<UpdateProviderServiceCommandHandler> _logger;

    public UpdateProviderServiceCommandHandler(
        IServiceWriteRepository serviceWriteRepository,
        IServiceReadRepository serviceReadRepository,
        ILogger<UpdateProviderServiceCommandHandler> logger)
    {
        _serviceWriteRepository = serviceWriteRepository;
        _serviceReadRepository = serviceReadRepository;
        _logger = logger;
    }

    public async Task<UpdateProviderServiceResult> Handle(
        UpdateProviderServiceCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating service {ServiceId} for provider {ProviderId}",
            request.ServiceId);

        // Validate request
        ValidateRequest(request);
        // Get service
        var serviceId = ServiceId.From(request.ServiceId);
        var service = await _serviceWriteRepository.GetByIdAsync(serviceId, cancellationToken);

        if (service == null)
        {
            throw new KeyNotFoundException($"Service with ID {request.ServiceId} not found");
        }

        // Verify service belongs to provider
        if (service.ProviderId.Value != request.ProviderId)
        {
            throw new UnauthorizedAccessException("Service does not belong to this provider");
        }

        // Check for duplicate name (excluding current service)
        var providerId = ProviderId.From(request.ProviderId);
        var isDuplicate = await _serviceReadRepository.ExistsWithNameForProviderAsync(
            providerId,
            request.ServiceName,
            serviceId,
            cancellationToken);

        if (isDuplicate)
        {
            throw new ValidationException("serviceName", $"A service with name '{request.ServiceName}' already exists for this provider");
        }

        // Update service
        var totalMinutes = (request.DurationHours * 60) + request.DurationMinutes;
        var duration = Duration.FromMinutes(totalMinutes);
        var price = Price.Create(request.Price, request.Currency);

        service.UpdateBasicInfo(
            request.ServiceName,
            request.Description ?? request.ServiceName,
            service.Category); // Keep existing category

        service.UpdatePricing(price);
        service.UpdateDuration(duration);

        // Save changes
        await _serviceWriteRepository.UpdateServiceAsync(service, cancellationToken);

        _logger.LogInformation(
            "Service {ServiceId} updated successfully",
            request.ServiceId);

        return new UpdateProviderServiceResult(
            service.Id.Value,
            service.Name,
            service.BasePrice.Amount,
            totalMinutes,
            DateTime.UtcNow);
    }

    private void ValidateRequest(UpdateProviderServiceCommand request)
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

        if (totalMinutes > 1440)
        {
            errors["duration"] = new List<string> { "Service duration cannot exceed 24 hours" };
        }

        if (request.Price < 0)
        {
            errors["price"] = new List<string> { "Service price cannot be negative" };
        }

        if (errors.Any())
        {
            throw new ValidationException(errors);
        }
    }
}

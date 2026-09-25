using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Abstractions.Services;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Service.UpdateProviderService;

public sealed class UpdateProviderServiceCommandHandler : ICommandHandler<UpdateProviderServiceCommand, UpdateProviderServiceResult>
{
    private readonly IServiceWriteRepository _serviceWriteRepository;
    private readonly IServiceReadRepository _serviceReadRepository;
    private readonly IProviderReadRepository _providerReadRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateProviderServiceCommandHandler> _logger;

    public UpdateProviderServiceCommandHandler(
        IServiceWriteRepository serviceWriteRepository,
        IServiceReadRepository serviceReadRepository,
        IProviderReadRepository providerReadRepository,
        ICurrentUserService currentUserService,
        ILogger<UpdateProviderServiceCommandHandler> logger)
    {
        _serviceWriteRepository = serviceWriteRepository;
        _serviceReadRepository = serviceReadRepository;
        _providerReadRepository = providerReadRepository;
        _currentUserService = currentUserService;
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
            throw new ForbiddenException("Service does not belong to this provider");
        }

        // The caller must own request.ProviderId (or be an admin) -- without this, any
        // authenticated Provider-role caller could pass someone else's providerId/serviceId
        // pair (the check above only proves the pair is internally consistent, not that the
        // caller has any right to it) and edit that provider's service. Confirmed reachable:
        // ServicesController's route for this command only requires the "ProviderOrAdmin"
        // policy (any provider), unlike ProviderSettingsController's equivalent route, which
        // separately checks CanManageProvider(id) before ever constructing this command.
        var isAdmin = _currentUserService.IsInRole("Admin")
            || _currentUserService.IsInRole("SysAdmin")
            || _currentUserService.IsInRole("Administrator");
        if (!isAdmin)
        {
            var callerId = UserId.From(_currentUserService.UserId
                ?? throw new UnauthorizedAccessException("User not authenticated"));
            var provider = await _providerReadRepository.GetByIdAsync(
                ProviderId.From(request.ProviderId), cancellationToken)
                ?? throw new KeyNotFoundException($"Provider with ID {request.ProviderId} not found");
            if (provider.OwnerId != callerId)
                throw new ForbiddenException("You are not authorized to manage this provider's services");
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

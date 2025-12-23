using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

public sealed class SaveStep4ServicesCommandHandler
    : ICommandHandler<SaveStep4ServicesCommand, SaveStep4ServicesResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IServiceWriteRepository _serviceRepository;
    private readonly IServiceReadRepository _serviceReadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SaveStep4ServicesCommandHandler(
        IProviderWriteRepository providerRepository,
        IServiceWriteRepository serviceRepository,
        IServiceReadRepository serviceReadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _providerRepository = providerRepository;
        _serviceRepository = serviceRepository;
        _serviceReadRepository = serviceReadRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<SaveStep4ServicesResult> Handle(
        SaveStep4ServicesCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(_currentUserService.UserId ??
            throw new UnauthorizedAccessException("User not authenticated"));

        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
            throw new InvalidOperationException("Provider not found");

        if (provider.OwnerId != userId)
            throw new UnauthorizedAccessException("You are not authorized to update this provider");

        if (provider.Status != ProviderStatus.Drafted)
            throw new InvalidOperationException("Provider is not in draft status");

        if (!request.Services.Any())
            throw new InvalidOperationException("At least one service is required");

        // Get existing services for this provider
        var existingServices = await _serviceReadRepository.GetByProviderIdAsync(providerId, cancellationToken);

        // Delete existing services (for draft providers only)
        foreach (var existingService in existingServices)
        {
            await _serviceRepository.DeleteServiceAsync(existingService, cancellationToken);
        }

        // Create new services
        foreach (var serviceDto in request.Services)
        {
            var totalMinutes = (serviceDto.DurationHours * 60) + serviceDto.DurationMinutes;
            var duration = Duration.FromMinutes(totalMinutes);

            // Default currency - could be made configurable
            var price = Price.Create(serviceDto.Price, "USD");

            // Determine service type based on price type
            var serviceType = serviceDto.PriceType.ToLower() == "fixed"
                ? ServiceType.Standard
                : ServiceType.Premium;

            var service = Domain.Aggregates.Service.Create(
                providerId,
                serviceDto.Name,
                "", // Description - could be added to DTO
                ServiceCategory.BeautySalon, // Default category
                serviceType,
                price,
                duration);

            await _serviceRepository.SaveServiceAsync(service, cancellationToken);
        }

        // Update registration step
        provider.UpdateRegistrationStep(4);

        await _unitOfWork.CommitAsync(cancellationToken);

        return new SaveStep4ServicesResult(
            provider.Id.Value,
            4,
            request.Services.Count,
            $"{request.Services.Count} service(s) saved successfully");
    }
}

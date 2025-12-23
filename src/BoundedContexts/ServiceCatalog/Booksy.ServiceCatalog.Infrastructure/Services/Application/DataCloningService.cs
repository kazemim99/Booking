using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Services.Application;

/// <summary>
/// Service for cloning provider data (services, hours, gallery) from organization to individual
/// </summary>
public class DataCloningService : IDataCloningService
{
    private readonly IServiceReadRepository _serviceReadRepository;
    private readonly IServiceWriteRepository _serviceWriteRepository;
    private readonly IProviderReadRepository _providerReadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DataCloningService> _logger;

    public DataCloningService(
        IServiceReadRepository serviceReadRepository,
        IServiceWriteRepository serviceWriteRepository,
        IProviderReadRepository providerReadRepository,
        IUnitOfWork unitOfWork,
        ILogger<DataCloningService> logger)
    {
        _serviceReadRepository = serviceReadRepository;
        _serviceWriteRepository = serviceWriteRepository;
        _providerReadRepository = providerReadRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> CloneServicesAsync(
        ProviderId sourceProviderId,
        Provider targetProvider,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Cloning services from provider {SourceId} to {TargetId}",
                sourceProviderId, targetProvider.Id);

            // Get all active services from source provider
            var sourceServices = await _serviceReadRepository.GetByProviderIdAsync(
                sourceProviderId.Value,
                cancellationToken);

            if (!sourceServices.Any())
            {
                _logger.LogInformation("No active services found to clone from provider {SourceId}", sourceProviderId);
                return 0;
            }

            int clonedCount = 0;

            foreach (var sourceService in sourceServices)
            {
                try
                {
                    // ServiceCategory is now an enum, so we can just copy it
                    var clonedCategory = sourceService.Category;

                    var clonedBasePrice = Price.Create(
                        sourceService.BasePrice.Amount,
                        sourceService.BasePrice.Currency);

                    var clonedDuration = Duration.FromMinutes(sourceService.Duration.Value);

                    // Create a new service for target provider with same properties
                    var clonedService = Service.Create(
                        providerId: targetProvider.Id,
                        name: sourceService.Name,
                        description: sourceService.Description,
                        category: clonedCategory,
                        type: sourceService.Type,
                        basePrice: clonedBasePrice,
                        duration: clonedDuration);

                    // Copy additional properties
                    if (sourceService.PreparationTime != null || sourceService.BufferTime != null)
                    {
                        clonedService.UpdateDuration(
                            sourceService.Duration,
                            sourceService.PreparationTime,
                            sourceService.BufferTime);
                    }

                    // Copy settings
                    if (sourceService.RequiresDeposit)
                    {
                        clonedService.EnableDeposit(sourceService.DepositPercentage);
                    }

                    clonedService.UpdateAvailability(
                        sourceService.AvailableAtLocation,
                        sourceService.AvailableAsMobile);

                    if (sourceService.MaxAdvanceBookingDays.HasValue &&
                        sourceService.MinAdvanceBookingHours.HasValue &&
                        sourceService.MaxConcurrentBookings.HasValue)
                    {
                        clonedService.UpdateBookingRules(
                            sourceService.MaxAdvanceBookingDays.Value,
                            sourceService.MinAdvanceBookingHours.Value,
                            sourceService.MaxConcurrentBookings.Value);
                    }

                    if (!string.IsNullOrEmpty(sourceService.ImageUrl))
                    {
                        clonedService.SetImage(sourceService.ImageUrl);
                    }

                    // Note: Service will remain in Draft status
                    // Individual provider can activate when ready

                    // Add the cloned service to repository
                    await _serviceWriteRepository.SaveAsync(clonedService, cancellationToken);

                    clonedCount++;

                    _logger.LogDebug("Cloned service {ServiceName} from {SourceId} to {TargetId}",
                        sourceService.Name, sourceProviderId, targetProvider.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cloning service {ServiceId} from {SourceId} to {TargetId}",
                        sourceService.Id, sourceProviderId, targetProvider.Id);
                    // Continue with next service
                }
            }

            _logger.LogInformation("Successfully cloned {Count} services from {SourceId} to {TargetId}",
                clonedCount, sourceProviderId, targetProvider.Id);

            return clonedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning services from {SourceId} to {TargetId}",
                sourceProviderId, targetProvider.Id);
            throw;
        }
    }

    public async Task<int> CloneWorkingHoursAsync(
        ProviderId sourceProviderId,
        Provider targetProvider,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Cloning working hours from provider {SourceId} to {TargetId}",
                sourceProviderId, targetProvider.Id);

            // Get source and target providers
            var sourceProvider = await _providerReadRepository.GetByIdAsync(sourceProviderId, cancellationToken);

            if (sourceProvider == null || targetProvider == null)
            {
                _logger.LogWarning("Source or target provider not found");
                return 0;
            }

            // Clone business hours using SetBusinessHours
            int clonedCount = 0;
            if (sourceProvider.BusinessHours.Any())
            {
                var hoursDict = new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>();

                foreach (var businessHour in sourceProvider.BusinessHours)
                {
                    if (businessHour.IsOpen) // Use IsOpen instead of IsClosed
                    {
                        hoursDict[businessHour.DayOfWeek] = (businessHour.OpenTime, businessHour.CloseTime);
                    }
                }

                if (hoursDict.Any())
                {
                    targetProvider.SetBusinessHours(hoursDict);
                    clonedCount = hoursDict.Count;
                }
            }

            // Clone holidays if any
            foreach (var holiday in sourceProvider.Holidays)
            {
                try
                {
                    targetProvider.AddHoliday(holiday.Date, holiday.Reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cloning holiday {HolidayReason}", holiday.Reason);
                }
            }

            _logger.LogInformation("Successfully cloned {Count} working hours entries from {SourceId} to {TargetId}",
                clonedCount, sourceProviderId, targetProvider.Id);

            return clonedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning working hours from {SourceId} to {TargetId}",
                sourceProviderId, targetProvider.Id);
            throw;
        }
    }

    public async Task<int> CloneGalleryAsync(
        ProviderId sourceProviderId,
        Provider targetProvider,
        bool markAsCloned = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Cloning gallery from provider {SourceId} to {TargetId}",
                sourceProviderId, targetProvider.Id);

            // Get source and target providers
            var sourceProvider = await _providerReadRepository.GetByIdAsync(sourceProviderId, cancellationToken);

            if (sourceProvider == null || targetProvider == null)
            {
                _logger.LogWarning("Source or target provider not found");
                return 0;
            }

            // Clone gallery images
            // Note: In a real implementation, this would clone from a Gallery aggregate
            // For now, we'll just log the intention
            // You would need to implement this based on your Gallery domain model

            int clonedCount = 0;

            // TODO: Implement gallery cloning when Gallery aggregate is available
            // Example:
            // var galleryImages = await _galleryRepository.GetByProviderIdAsync(sourceProviderId, cancellationToken);
            // foreach (var image in galleryImages)
            // {
            //     var clonedImage = GalleryImage.Create(
            //         targetProviderId,
            //         image.ImageUrl,
            //         image.Caption,
            //         markAsCloned,
            //         sourceProviderId);
            //     await _galleryRepository.AddAsync(clonedImage, cancellationToken);
            //     clonedCount++;
            // }

            _logger.LogInformation(
                "Gallery cloning placeholder - {Count} images would be cloned from {SourceId} to {TargetId}",
                clonedCount, sourceProviderId, targetProvider.Id);

            return clonedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning gallery from {SourceId} to {TargetId}",
                sourceProviderId, targetProvider.Id);
            throw;
        }
    }
}

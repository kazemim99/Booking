// ========================================
// Booksy.ServiceCatalog.Application/Services/Implementations/ServiceApplicationService.cs
// ========================================
using Booksy.ServiceCatalog.Application.DTOs.Service;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Services.Implementations
{
    public sealed class ServiceApplicationService : IServiceApplicationService
    {
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<ServiceApplicationService> _logger;

        public ServiceApplicationService(
            IServiceReadRepository serviceRepository,
            ILogger<ServiceApplicationService> logger)
        {
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        public async Task<ServiceDto?> GetServiceByIdAsync(ServiceId serviceId, CancellationToken cancellationToken = default)
        {
            var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service == null) return null;

            return new ServiceDto
            {
                Id = service.Id.Value,
                ProviderId = service.ProviderId.Value,
                Name = service.Name,
                Description = service.Description,
                Category = service.Category.Name,
                Type = service.Type,
                BasePrice = service.BasePrice.Amount,
                Currency = service.BasePrice.Currency,
                Duration = service.Duration.Value,
                PreparationTime = service.PreparationTime?.Value,
                BufferTime = service.BufferTime?.Value,
                Status = service.Status,
                RequiresDeposit = service.RequiresDeposit,
                DepositPercentage = service.DepositPercentage,
                AllowOnlineBooking = service.AllowOnlineBooking,
                AvailableAtLocation = service.AvailableAtLocation,
                AvailableAsMobile = service.AvailableAsMobile,
                MaxAdvanceBookingDays = service.MaxAdvanceBookingDays ?? 90,
                MinAdvanceBookingHours = service.MinAdvanceBookingHours ?? 1,
                MaxConcurrentBookings = service.MaxConcurrentBookings ?? 1,
                ImageUrl = service.ImageUrl,
                CreatedAt = service.CreatedAt,
                ActivatedAt = service.ActivatedAt
            };
        }

        public async Task<IReadOnlyList<ServiceSummaryDto>> GetServicesByProviderAsync(
            ProviderId providerId,
            ServiceStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var services = status.HasValue
                ? await _serviceRepository.GetByProviderIdAndStatusAsync(providerId, status.Value, cancellationToken)
                : await _serviceRepository.GetByProviderIdAsync(providerId, cancellationToken);

            return services.Select(service => new ServiceSummaryDto
            {
                Id = service.Id.Value,
                Name = service.Name,
                Description = service.Description,
                Category = service.Category.Name,
                BasePrice = service.BasePrice.Amount,
                Currency = service.BasePrice.Currency,
                Duration = service.Duration.Value,
                Status = service.Status,
                ImageUrl = service.ImageUrl,
            }).ToList();
        }

        public async Task<IReadOnlyList<ServiceSummaryDto>> GetServicesByCategoryAsync(
            string category,
            CancellationToken cancellationToken = default)
        {

            var services = await _serviceRepository
                .GetByCategoryAsync(category, cancellationToken);

            return services.Select(service => new ServiceSummaryDto
            {
                Id = service.Id.Value,
                Name = service.Name,
                Description = service.Description,
                Category = service.Category.Name,
                BasePrice = service.BasePrice.Amount,
                Currency = service.BasePrice.Currency,
                Duration = service.Duration.Value,
                Status = service.Status,
                ImageUrl = service.ImageUrl,
            }).ToList();
        }

        public async Task<IReadOnlyList<ServiceSummaryDto>> SearchServicesAsync(
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            var services = await _serviceRepository.SearchAsync(searchTerm, cancellationToken);

            return services.Select(service => new ServiceSummaryDto
            {
                Id = service.Id.Value,
                Name = service.Name,
                Description = service.Description,
                Category = service.Category.Name,
                BasePrice = service.BasePrice.Amount,
                Currency = service.BasePrice.Currency,
                Duration = service.Duration.Value,
                Status = service.Status,
                ImageUrl = service.ImageUrl,
            }).ToList();
        }

        public async Task<bool> IsServiceNameUniqueForProviderAsync(
            ProviderId providerId,
            string serviceName,
            ServiceId? excludeServiceId = null,
            CancellationToken cancellationToken = default)
        {
            return !await _serviceRepository.ExistsWithNameForProviderAsync(
                providerId,
                serviceName,
                excludeServiceId,
                cancellationToken);
        }

        public async Task<ServiceStatisticsDto> GetServiceStatisticsAsync(
            ServiceId serviceId,
            CancellationToken cancellationToken = default)
        {
            var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service == null)
                throw new InvalidOperationException("Service not found");

            return new ServiceStatisticsDto
            {
                ServiceId = service.Id.Value,
                ServiceName = service.Name,
                TotalBookings = 0, // Would be populated from Booking context
                AverageRating = 0.0m, // Would be populated from Reviews context
                Revenue = 0.0m, // Would be populated from Payment context
                LastBookingDate = null // Would be populated from Booking context
            };
        }

        public async Task<bool> CanServiceBeActivatedAsync(
            ServiceId serviceId,
            CancellationToken cancellationToken = default)
        {
            var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);
            return service?.CanBeBooked() ?? false;
        }

        public async Task<Price> CalculateServicePriceAsync(
            ServiceId serviceId,
            IEnumerable<Guid> selectedOptionIds,
            Guid? selectedTierId = null,
            CancellationToken cancellationToken = default)
        {
            var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service == null)
                throw new InvalidOperationException("Service not found");

            var basePrice = service.BasePrice;

            // Add option prices
            foreach (var optionId in selectedOptionIds)
            {
                var option = service.Options.FirstOrDefault(o => o.Id == optionId);
                if (option != null)
                {
                    basePrice += option.AdditionalPrice;
                }
            }

            // Use tier price if specified
            if (selectedTierId.HasValue)
            {
                var tier = service.PriceTiers.FirstOrDefault(t => t.Id == selectedTierId.Value);
                if (tier != null)
                {
                    basePrice = tier.Price;
                }
            }

            return basePrice;
        }
    }
}
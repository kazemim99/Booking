// ========================================
// Booksy.ServiceCatalog.Application/Services/Implementations/ProviderApplicationService.cs
// ========================================
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Application.Mappings;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Services.Implementations
{
    public sealed class ProviderApplicationService : IProviderApplicationService
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<ProviderApplicationService> _logger;

        public ProviderApplicationService(
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            ILogger<ProviderApplicationService> logger)
        {
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        public async Task<ProviderDto?> GetProviderByIdAsync(ProviderId providerId, CancellationToken cancellationToken = default)
        {
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
            return provider?.ToDto();
        }

        public async Task<ProviderDto?> GetProviderByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            var provider = await _providerRepository.GetByOwnerIdAsync(
                Core.Domain.ValueObjects.UserId.From(ownerId), cancellationToken);
            return provider?.ToDto();
        }

        public async Task<IReadOnlyList<ProviderSummaryDto>> GetProvidersByStatusAsync(ProviderStatus status, CancellationToken cancellationToken = default)
        {
            var providers = await _providerRepository.GetByStatusAsync(status, cancellationToken);
            return providers.Select(p => p.ToSummaryDto()).ToList();
        }

        public async Task<IReadOnlyList<ProviderSummaryDto>> SearchProvidersAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var providers = await _providerRepository.SearchAsync(searchTerm, cancellationToken);
            return providers.Select(p => p.ToSummaryDto()).ToList();
        }

        public async Task<IReadOnlyList<ProviderSummaryDto>> GetProvidersByLocationAsync(
            double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default)
        {
            var providers = await _providerRepository.GetByLocationAsync(latitude, longitude, radiusKm, cancellationToken);
            return providers.Select(p => p.ToSummaryDto()).ToList();
        }

        public async Task<bool> IsBusinessNameUniqueAsync(string businessName, ProviderId? excludeProviderId = null, CancellationToken cancellationToken = default)
        {
            return !await _providerRepository.ExistsByBusinessNameAsync(businessName, excludeProviderId, cancellationToken);
        }

        public async Task<ProviderStatisticsDto> GetProviderStatisticsAsync(ProviderId providerId, CancellationToken cancellationToken = default)
        {
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
            if (provider == null)
                throw new InvalidOperationException("Provider not found");

            var services = await _serviceRepository.GetByProviderIdAsync(providerId, cancellationToken);
            var activeServices = services.Count(s => s.Status == ServiceStatus.Active);
            var totalBookings = 0; // Would come from Booking context integration
            var totalRevenue = 0m; // Would come from Payment context integration

            return new ProviderStatisticsDto
            {
                ProviderId = provider.Id.Value,
                BusinessName = provider.Profile.BusinessName,
                TotalServices = services.Count,
                ActiveServices = activeServices,
                TotalBookings = totalBookings,
                TotalRevenue = totalRevenue,
                AverageRating = 0.0m, // Would come from Reviews context
                RegisteredAt = provider.RegisteredAt,
                LastActiveAt = provider.LastActiveAt
            };
        }

        public async Task<bool> CanProviderBeActivatedAsync(ProviderId providerId, CancellationToken cancellationToken = default)
        {
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

            if (provider == null) return false;
            if (provider.Status == ProviderStatus.Active) return false;
            if (provider.Status == ProviderStatus.Suspended) return false;

            // Business rules for activation
            var hasRequiredInfo = !string.IsNullOrEmpty(provider.Profile.BusinessName) &&
                                 !string.IsNullOrEmpty(provider.Profile.BusinessDescription) &&
                                 provider.ContactInfo != null &&
                                 provider.Address != null;

            return hasRequiredInfo;
        }
    }

    public static class ProviderMappingExtensions
    {
        public static ProviderDto ToDto(this Domain.Aggregates.Provider provider)
        {
            return new ProviderDto
            {
                Id = provider.Id.Value,
                OwnerId = provider.OwnerId.Value,
                Profile = new BusinessProfileDto
                {
                    BusinessName = provider.Profile.BusinessName,
                    Description = provider.Profile.BusinessDescription,
                    Website = provider.Profile.Website,
                    LogoUrl = provider.Profile.LogoUrl,
                    SocialMedia = provider.Profile.SocialMedia,
                    Tags = provider.Profile.Tags,
                    LastUpdatedAt = provider.Profile.LastUpdatedAt
                },
                Status = provider.Status,
                PrimaryCategory = provider.PrimaryCategory,
                RequiresApproval = provider.RequiresApproval,
                AllowOnlineBooking = provider.AllowOnlineBooking,
                OffersMobileServices = provider.OffersMobileServices,
                RegisteredAt = provider.RegisteredAt,
                ActivatedAt = provider.ActivatedAt,
                VerifiedAt = provider.VerifiedAt,
                LastActiveAt = provider.LastActiveAt
            };
        }
    }
}
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.Services;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Services.Domain
{
    public sealed class BusinessRuleService : IBusinessRuleService
    {
        private readonly IProviderReadRepository _providerReadRepository;
        private readonly IServiceReadRepository _serviceReadRepository;
        private readonly ILogger<BusinessRuleService> _logger;

        public BusinessRuleService(
            IProviderReadRepository providerReadRepository,
            IServiceReadRepository serviceReadRepository,
            ILogger<BusinessRuleService> logger)
        {
            _providerReadRepository = providerReadRepository;
            _serviceReadRepository = serviceReadRepository;
            _logger = logger;
        }

        public async Task<bool> CanProviderOfferServiceAsync(
            ProviderId providerId,
            ServiceId serviceId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var provider = await _providerReadRepository.GetByIdAsync(providerId, cancellationToken);
                if (provider == null)
                {
                    _logger.LogWarning("Provider {ProviderId} not found", providerId);
                    return false;
                }

                var service = await _serviceReadRepository.GetByIdAsync(serviceId, cancellationToken);
                if (service == null)
                {
                    _logger.LogWarning("Service {ServiceId} not found", serviceId);
                    return false;
                }

                // Business rule: Provider must be active and verified to offer services
                if (provider.Status != ProviderStatus.Active)
                {
                    _logger.LogInformation("Provider {ProviderId} is not active (Status: {Status})",
                        providerId, provider.Status);
                    return false;
                }

                // Business rule: Service must belong to the provider
                if (service.ProviderId != providerId)
                {
                    _logger.LogWarning("Service {ServiceId} does not belong to provider {ProviderId}",
                        serviceId, providerId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if provider {ProviderId} can offer service {ServiceId}",
                    providerId, serviceId);
                return false;
            }
        }

        public async Task<bool> IsServiceNameUniqueForProviderAsync(
            ProviderId providerId,
            string serviceName,
            ServiceId? excludeServiceId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return !await _serviceReadRepository.ExistsWithNameForProviderAsync(
                    providerId, serviceName, excludeServiceId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking service name uniqueness for provider {ProviderId}", providerId);
                return false;
            }
        }

        public async Task<bool> CanStaffProvideServiceAsync(
            Guid staffId,
            ServiceId serviceId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // This would typically involve checking staff qualifications, certifications, etc.
                // For now, implementing basic logic
                //var service = await _serviceReadRepository.GetByIdAsync(serviceId, cancellationToken);
                //if (service == null)
                //    return false;

                //// Check if staff is qualified to provide this service
                //// This would be implemented based on your business rules
                //return service.QualifiedStaff.Contains(staffId);

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if staff {StaffId} can provide service {ServiceId}",
                    staffId, serviceId);
                return false;
            }
        }

        public async Task<bool> IsProviderEligibleForVerificationAsync(
            ProviderId providerId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var provider = await _providerReadRepository.GetByIdAsync(providerId, cancellationToken);
                if (provider == null)
                    return false;

                // Business rules for verification eligibility
                var servicesCount = await _serviceReadRepository.CountByProviderAsync(
                    providerId, ServiceStatus.Active, cancellationToken);

                // Example rules:
                // - Must have at least one active service
                // - Must be registered for at least 30 days
                // - Must have complete business profile
                return servicesCount > 0 &&
                       provider.RegisteredAt <= DateTime.UtcNow.AddDays(-30) &&
                       !string.IsNullOrEmpty(provider.Profile.BusinessName) &&
                       provider.ContactInfo.Email != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking provider verification eligibility {ProviderId}", providerId);
                return false;
            }
        }

        public async Task<int> GetMaxServicesForProviderTypeAsync(
            Provider provider,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Business rules for maximum services based on provider hierarchy type
                // Organizations can have more services than individuals
                return provider.HierarchyType switch
                {
                    ProviderHierarchyType.Individual => 20,
                    ProviderHierarchyType.Organization => 200,
                    _ => 50
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting max services for provider {ProviderId}", provider.Id);
                return 10; // Default fallback
            }
        }

        public async Task<bool> ValidateServicePricingRulesAsync(
            Service service,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Business rules for service pricing
                // - Minimum price validation
                // - Currency consistency
                // - Deposit percentage validation

                if (service.BasePrice.Amount < 0)
                    return false;

                if (service.RequiresDeposit && (service.DepositPercentage < 0 || service.DepositPercentage > 100))
                    return false;

                // Check price tiers consistency
                foreach (var priceTier in service.PriceTiers)
                {
                    if (priceTier.Price.Currency != service.BasePrice.Currency)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating service pricing rules for service {ServiceId}", service.Id);
                return false;
            }
        }
    }
}

// ========================================
// AsanRezerve.ServiceCatalog.Domain/Services/IBusinessRuleService.cs
// ========================================
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Services
{
    /// <summary>
    /// Domain service for complex business rules that span multiple aggregates
    /// </summary>
    public interface IBusinessRuleService
    {
        Task<bool> CanProviderOfferServiceAsync(ProviderId providerId, ServiceId serviceId, CancellationToken cancellationToken = default);
        Task<bool> IsServiceNameUniqueForProviderAsync(ProviderId providerId, string serviceName, ServiceId? excludeServiceId = null, CancellationToken cancellationToken = default);
        Task<bool> CanStaffProvideServiceAsync(Guid staffId, ServiceId serviceId, CancellationToken cancellationToken = default);
        Task<bool> IsProviderEligibleForVerificationAsync(ProviderId providerId, CancellationToken cancellationToken = default);
        Task<int> GetMaxServicesForProviderCategoryAsync(Provider provider, CancellationToken cancellationToken = default);
        Task<bool> ValidateServicePricingRulesAsync(Service service, CancellationToken cancellationToken = default);
    }
}
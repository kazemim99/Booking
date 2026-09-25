// ========================================
// AsanRezerve.ServiceCatalog.Domain/Services/IProviderValidationService.cs
// ========================================
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Services
{
    /// <summary>
    /// Domain service for provider validation rules
    /// </summary>
    public interface IProviderValidationService
    {
        Task<bool> IsBusinessNameUniqueAsync(string businessName, ProviderId? excludeProviderId = null, CancellationToken cancellationToken = default);
        Task<bool> IsBusinessEmailUniqueAsync(Email email, ProviderId? excludeProviderId = null, CancellationToken cancellationToken = default);
        Task<bool> ValidateBusinessAddressAsync(BusinessAddress address, CancellationToken cancellationToken = default);
        Task<bool> IsProviderEligibleForActivationAsync(ProviderId providerId, CancellationToken cancellationToken = default);
        Task<bool> ValidateBusinessHoursAsync(Provider provider, CancellationToken cancellationToken = default);
        Task<bool> CanProviderAddMoreStaffAsync(ProviderId providerId, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetProviderValidationErrorsAsync(Provider provider, CancellationToken cancellationToken = default);
    }
}
// ========================================
// Booksy.ServiceCatalog.Domain/Services/IProviderValidationService.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Services
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
// ========================================
// Booksy.ServiceCatalog.Application/Services/Interfaces/IProviderApplicationService.cs
// ========================================
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Services.Interfaces
{
    /// <summary>
    /// Application service for provider-related operations
    /// </summary>
    public interface IProviderApplicationService
    {
        Task<ProviderDto?> GetProviderByIdAsync(ProviderId providerId, CancellationToken cancellationToken = default);
        Task<ProviderDto?> GetProviderByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProviderSummaryDto>> GetProvidersByStatusAsync(ProviderStatus status, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProviderSummaryDto>> SearchProvidersAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProviderSummaryDto>> GetProvidersByLocationAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default);
        Task<bool> IsBusinessNameUniqueAsync(string businessName, ProviderId? excludeProviderId = null, CancellationToken cancellationToken = default);
        Task<ProviderStatisticsDto> GetProviderStatisticsAsync(ProviderId providerId, CancellationToken cancellationToken = default);
        Task<bool> CanProviderBeActivatedAsync(ProviderId providerId, CancellationToken cancellationToken = default);
    }
}
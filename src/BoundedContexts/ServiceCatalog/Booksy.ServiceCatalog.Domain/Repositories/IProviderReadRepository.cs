// ========================================
// Booksy.ServiceCatalog.Domain/Repositories/IProviderReadRepository.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    public interface IProviderReadRepository : IReadRepository<Provider, ProviderId>
    {
        Task<Provider?> GetByOwnerIdAsync(UserId ownerId, CancellationToken cancellationToken = default);
        Task<Provider?> GetByBusinessNameAsync(string businessName, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Provider>> GetByStatusAsync(ProviderStatus status, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Provider>> GetByCategoryAsync(ServiceCategory category, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Provider>> GetByCityAsync(string city, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Provider>> GetByLocationAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Provider>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<PagedResult<Provider>> GetPaginatedAsync(int pageNumber, int pageSize, ProviderStatus? status = null, ServiceCategory? type = null, CancellationToken cancellationToken = default);
        Task<bool> ExistsByBusinessNameAsync(string businessName, ProviderId? excludeId = null, CancellationToken cancellationToken = default);
        Task<bool> ExistsByOwnerIdAsync(UserId ownerId, CancellationToken cancellationToken = default);
        Task<int> CountByStatusAsync(ProviderStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts providers per primary category for a given status, aggregated in the database.
        /// Backs the category browse/popular endpoints, which would otherwise have to materialise
        /// every active provider just to group them in memory.
        /// Categories with no providers are absent from the result rather than mapped to zero.
        /// </summary>
        Task<IReadOnlyDictionary<ServiceCategory, int>> CountByCategoryAsync(ProviderStatus status, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Provider>> GetRecentlyActiveAsync(int count, CancellationToken cancellationToken = default);

        // Hierarchy-related methods
        Task<IReadOnlyList<Provider>> GetStaffByOrganizationIdAsync(ProviderId organizationId, CancellationToken cancellationToken = default);
        Task<Provider?> GetOrganizationByStaffIdAsync(ProviderId staffProviderId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Provider>> GetByHierarchyTypeAsync(ProviderHierarchyType hierarchyType, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Provider>> GetIndependentIndividualsAsync(CancellationToken cancellationToken = default);
        Task<int> CountStaffByOrganizationAsync(ProviderId organizationId, CancellationToken cancellationToken = default);
    }
}
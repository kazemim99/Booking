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
        Task<IReadOnlyList<Provider>> GetByTypeAsync(ProviderType type, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Provider>> GetByCityAsync(string city, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Provider>> GetByLocationAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Provider>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<PagedResult<Provider>> GetPaginatedAsync(int pageNumber, int pageSize, ProviderStatus? status = null, ProviderType? type = null, CancellationToken cancellationToken = default);
        Task<bool> ExistsByBusinessNameAsync(string businessName, ProviderId? excludeId = null, CancellationToken cancellationToken = default);
        Task<bool> ExistsByOwnerIdAsync(UserId ownerId, CancellationToken cancellationToken = default);
        Task<int> CountByStatusAsync(ProviderStatus status, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Provider>> GetRecentlyActiveAsync(int count, CancellationToken cancellationToken = default);
    }
}
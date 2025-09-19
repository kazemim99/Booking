// ========================================
// Booksy.ServiceCatalog.Domain/Repositories/IServiceReadRepository.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    public interface IServiceReadRepository : IReadRepository<Service, ServiceId>
    {
        Task<IReadOnlyList<Service>> GetByProviderIdAsync(ProviderId providerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Service>> GetByProviderIdAndStatusAsync(ProviderId providerId, ServiceStatus status, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Service>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Service>> GetByStatusAsync(ServiceStatus status, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Service>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Service>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, string currency, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Service>> GetByDurationRangeAsync(int minMinutes, int maxMinutes, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Service>> GetMobileServicesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Service>> GetServicesRequiringDepositAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<Service>> GetPaginatedAsync(int pageNumber, int pageSize, ServiceStatus? status = null, ServiceCategory? category = null, ProviderId? providerId = null, CancellationToken cancellationToken = default);
        Task<bool> ExistsWithNameForProviderAsync(ProviderId providerId, string serviceName, ServiceId? excludeId = null, CancellationToken cancellationToken = default);
        Task<int> CountByProviderAsync(ProviderId providerId, ServiceStatus? status = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Service>> GetPopularServicesAsync(int count, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Service>> GetServicesByTagAsync(string tag, CancellationToken cancellationToken = default);
        Task<decimal> GetAveragePriceByCategoryAsync(string category, string currency, CancellationToken cancellationToken = default);
    }
}
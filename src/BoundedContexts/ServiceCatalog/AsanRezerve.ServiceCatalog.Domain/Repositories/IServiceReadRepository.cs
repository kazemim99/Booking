// ========================================
// AsanRezerve.ServiceCatalog.Domain/Repositories/IServiceReadRepository.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Application.DTOs;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Repositories
{
    public interface IServiceReadRepository : IReadRepository<Service, ServiceId>
    {
        Task<IReadOnlyList<Service>> GetByProviderIdAsync(ProviderId providerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Service>> GetByProviderIdAndStatusAsync(ProviderId providerId, ServiceStatus status, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Service>> GetByCategoryAsync(ServiceCategory category, CancellationToken cancellationToken = default);
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
        Task<decimal> GetAveragePriceByCategoryAsync(ServiceCategory category, string currency, CancellationToken cancellationToken = default);
    }
}
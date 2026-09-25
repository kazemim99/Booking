// ========================================
// AsanRezerve.ServiceCatalog.Domain/Repositories/IServiceWriteRepository.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Repositories
{
    public interface IServiceWriteRepository : IWriteRepository<Service, ServiceId>
    {
        Task<Service?> GetByIdAsync(ServiceId id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Service>> GetServicesByProviderIdAsync(ProviderId providerId, CancellationToken cancellationToken = default);
        Task SaveServiceAsync(Service service, CancellationToken cancellationToken = default);
        Task UpdateServiceAsync(Service service, CancellationToken cancellationToken = default);
        Task DeleteServiceAsync(Service service, CancellationToken cancellationToken = default);
    }
}
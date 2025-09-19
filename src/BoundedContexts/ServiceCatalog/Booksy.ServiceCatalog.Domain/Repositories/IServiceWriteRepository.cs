// ========================================
// Booksy.ServiceCatalog.Domain/Repositories/IServiceWriteRepository.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    public interface IServiceWriteRepository : IWriteRepository<Service, ServiceId>
    {
        Task<Service?> GetByIdAsync(ServiceId id, CancellationToken cancellationToken = default);
        Task SaveServiceAsync(Service service, CancellationToken cancellationToken = default);
        Task UpdateServiceAsync(Service service, CancellationToken cancellationToken = default);
        Task DeleteServiceAsync(Service service, CancellationToken cancellationToken = default);
    }
}
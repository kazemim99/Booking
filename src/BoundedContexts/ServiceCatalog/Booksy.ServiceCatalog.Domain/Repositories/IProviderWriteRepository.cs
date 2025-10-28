// ========================================
// Booksy.ServiceCatalog.Domain/Repositories/IProviderWriteRepository.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    public interface IProviderWriteRepository : IWriteRepository<Provider, ProviderId>
    {
        Task<Provider?> GetByIdAsync(ProviderId id, CancellationToken cancellationToken = default);
        Task<Provider?> GetByOwnerIdAsync(UserId id, CancellationToken cancellationToken = default);
        Task SaveProviderAsync(Provider provider, CancellationToken cancellationToken = default);
        Task UpdateProviderAsync(Provider provider, CancellationToken cancellationToken = default);
        Task DeleteProviderAsync(Provider provider, CancellationToken cancellationToken = default);
    }
}
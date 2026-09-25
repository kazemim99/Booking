// ========================================
// AsanRezerve.ServiceCatalog.Domain/Repositories/IProviderWriteRepository.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Repositories
{
    public interface IProviderWriteRepository : IWriteRepository<Provider, ProviderId>
    {
        Task<Provider?> GetByIdAsync(ProviderId id, CancellationToken cancellationToken = default);
        Task<Provider?> GetByOwnerIdAsync(UserId id, CancellationToken cancellationToken = default);
        Task<Provider?> GetDraftProviderByOwnerIdAsync(UserId ownerId, CancellationToken cancellationToken = default);
        Task SaveProviderAsync(Provider provider, CancellationToken cancellationToken = default);
        Task UpdateProviderAsync(Provider provider, CancellationToken cancellationToken = default);
        Task DeleteProviderAsync(Provider provider, CancellationToken cancellationToken = default);
    }
}
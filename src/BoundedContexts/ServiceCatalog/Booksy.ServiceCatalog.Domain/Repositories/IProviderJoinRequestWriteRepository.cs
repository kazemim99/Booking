using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    public interface IProviderJoinRequestWriteRepository : IWriteRepository<ProviderJoinRequest, Guid>
    {
        Task SaveAsync(ProviderJoinRequest request, CancellationToken cancellationToken = default);
        Task UpdateAsync(ProviderJoinRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(ProviderJoinRequest request, CancellationToken cancellationToken = default);
    }
}

using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;

namespace AsanRezerve.ServiceCatalog.Domain.Repositories
{
    public interface IProviderInvitationWriteRepository : IWriteRepository<ProviderInvitation, Guid>
    {
        Task SaveAsync(ProviderInvitation invitation, CancellationToken cancellationToken = default);
        Task UpdateAsync(ProviderInvitation invitation, CancellationToken cancellationToken = default);
        Task DeleteAsync(ProviderInvitation invitation, CancellationToken cancellationToken = default);
    }
}

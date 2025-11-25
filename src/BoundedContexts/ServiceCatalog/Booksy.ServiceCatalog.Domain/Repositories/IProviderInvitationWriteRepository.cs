using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    public interface IProviderInvitationWriteRepository : IWriteRepository<ProviderInvitation, Guid>
    {
        Task SaveAsync(ProviderInvitation invitation, CancellationToken cancellationToken = default);
        Task UpdateAsync(ProviderInvitation invitation, CancellationToken cancellationToken = default);
        Task DeleteAsync(ProviderInvitation invitation, CancellationToken cancellationToken = default);
    }
}

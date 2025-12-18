using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class ProviderInvitationWriteRepository : EfWriteRepositoryBase<ProviderInvitation, Guid, ServiceCatalogDbContext>, IProviderInvitationWriteRepository
    {
        public ProviderInvitationWriteRepository(
            ServiceCatalogDbContext context,
            ILogger<ProviderInvitationWriteRepository> logger)
            : base(context)
        {
        }

        public async Task SaveAsync(ProviderInvitation invitation, CancellationToken cancellationToken = default)
        {
            await DbSet.AddAsync(invitation, cancellationToken);
            // Note: Do NOT call SaveChangesAsync here
            // The UnitOfWork handles saving and dispatching domain events
        }

        public new Task UpdateAsync(ProviderInvitation invitation, CancellationToken cancellationToken = default)
        {
            DbSet.Update(invitation);
            // Note: Do NOT call SaveChangesAsync here
            // The UnitOfWork handles saving and dispatching domain events
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ProviderInvitation invitation, CancellationToken cancellationToken = default)
        {
            DbSet.Remove(invitation);
            // Note: Do NOT call SaveChangesAsync here
            // The UnitOfWork handles saving and dispatching domain events
            return Task.CompletedTask;
        }
    }
}

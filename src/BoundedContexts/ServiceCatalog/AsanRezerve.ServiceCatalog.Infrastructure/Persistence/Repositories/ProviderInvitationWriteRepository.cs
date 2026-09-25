using AsanRezerve.Infrastructure.Core.Persistence.Base;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Repositories
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

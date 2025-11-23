using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class ProviderJoinRequestWriteRepository : EfWriteRepositoryBase<ProviderJoinRequest, Guid, ServiceCatalogDbContext>, IProviderJoinRequestWriteRepository
    {
        public ProviderJoinRequestWriteRepository(
            ServiceCatalogDbContext context,
            ILogger<ProviderJoinRequestWriteRepository> logger)
            : base(context)
        {
        }

        public async Task SaveAsync(ProviderJoinRequest request, CancellationToken cancellationToken = default)
        {
            await DbSet.AddAsync(request, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(ProviderJoinRequest request, CancellationToken cancellationToken = default)
        {
            DbSet.Update(request);
            await Context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(ProviderJoinRequest request, CancellationToken cancellationToken = default)
        {
            DbSet.Remove(request);
            await Context.SaveChangesAsync(cancellationToken);
        }
    }
}

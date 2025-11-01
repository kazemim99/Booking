using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class ProviderWriteRepository : EfWriteRepositoryBase<Provider, ProviderId, ServiceCatalogDbContext>, IProviderWriteRepository
    {
        public ProviderWriteRepository(
            ServiceCatalogDbContext context,
            ILogger<ProviderWriteRepository> logger)
            : base(context)
        {
        }

        public new async Task<Provider?> GetByIdAsync(ProviderId id, CancellationToken cancellationToken = default)
        {
            // Include all navigation properties and owned collections
            return await DbSet
                .Include(p => p.Staff)
                .Include(p => p.BusinessHours)
                .Include(p => p.Holidays)
                .Include(p => p.Exceptions)
                .Include(p => p.Profile)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public new async Task<Provider?> GetByOwnerIdAsync(UserId id, CancellationToken cancellationToken = default)
        {
            // Include all navigation properties and owned collections
            return await DbSet
                .Include(p => p.Staff)
                .Include(p => p.BusinessHours)
                .Include(p => p.Holidays)
                .Include(p => p.Exceptions)
                .Include(p => p.Profile)
                .FirstOrDefaultAsync(p => p.OwnerId == id, cancellationToken);
        }
        public async Task SaveProviderAsync(Provider provider, CancellationToken cancellationToken = default)
        {
            await SaveAsync(provider, cancellationToken);
        }

        public async Task UpdateProviderAsync(Provider provider, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

        public async Task DeleteProviderAsync(Provider provider, CancellationToken cancellationToken = default)
        {
            await RemoveAsync(provider, cancellationToken);
        }
    }
}

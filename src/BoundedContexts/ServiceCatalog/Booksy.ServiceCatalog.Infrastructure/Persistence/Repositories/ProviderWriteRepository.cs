using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

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

        public async Task<Provider?> GetByIdAsync(ProviderId id, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.Staff)
                .Include(p => p.BusinessHours)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task SaveProviderAsync(Provider provider, CancellationToken cancellationToken = default)
        {
            await SaveAsync(provider, cancellationToken);
        }

        public async Task UpdateProviderAsync(Provider provider, CancellationToken cancellationToken = default)
        {
            await UpdateAsync(provider, cancellationToken);
        }

        public async Task DeleteProviderAsync(Provider provider, CancellationToken cancellationToken = default)
        {
            await RemoveAsync(provider, cancellationToken);
        }
    }
}

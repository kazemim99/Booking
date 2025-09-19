using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class ServiceWriteRepository : EfWriteRepositoryBase<Service, ServiceId, ServiceCatalogDbContext>, IServiceWriteRepository
    {
        public ServiceWriteRepository(
            ServiceCatalogDbContext context,
            ILogger<ServiceWriteRepository> logger)
            : base(context)
        {
        }

        public async Task<Service?> GetByIdAsync(ServiceId id, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(s => s.Options)
                .Include(s => s.PriceTiers)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task SaveServiceAsync(Service service, CancellationToken cancellationToken = default)
        {
            await SaveAsync(service, cancellationToken);
        }

        public async Task UpdateServiceAsync(Service service, CancellationToken cancellationToken = default)
        {
            await UpdateAsync(service, cancellationToken);
        }

        public async Task DeleteServiceAsync(Service service, CancellationToken cancellationToken = default)
        {
            await RemoveAsync(service, cancellationToken);
        }
    }
}

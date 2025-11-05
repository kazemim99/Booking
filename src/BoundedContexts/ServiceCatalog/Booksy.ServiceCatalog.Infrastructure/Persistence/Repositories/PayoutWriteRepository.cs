// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/PayoutWriteRepository.cs
// ========================================
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.PayoutAggregate;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class PayoutWriteRepository : EfWriteRepositoryBase<Payout, PayoutId, ServiceCatalogDbContext>, IPayoutWriteRepository
    {
        public PayoutWriteRepository(ServiceCatalogDbContext context)
            : base(context)
        {
        }

        public new async Task<Payout?> GetByIdAsync(PayoutId id, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public new async Task AddAsync(Payout payout, CancellationToken cancellationToken = default)
        {
            await DbSet.AddAsync(payout, cancellationToken);
        }

        public async Task UpdateAsync(Payout payout, CancellationToken cancellationToken = default)
        {
            Context.Update(payout);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(PayoutId id, CancellationToken cancellationToken = default)
        {
            return await DbSet.AnyAsync(p => p.Id == id, cancellationToken);
        }
    }
}

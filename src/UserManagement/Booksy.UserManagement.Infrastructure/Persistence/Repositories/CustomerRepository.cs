// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Repositories/CustomerRepository.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.UserManagement.Domain.Aggregates.CustomerAggregate;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booksy.UserManagement.Infrastructure.Persistence.Repositories;

public class CustomerRepository : EfRepositoryBase<Customer, CustomerId, UserManagementDbContext>, ICustomerRepository
{
    public CustomerRepository(UserManagementDbContext context) : base(context) { }

    // Domain-specific methods
    public async Task<Customer?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        return await DbSet
            .Include(c => c.FavoriteProviders)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task<bool> ExistsByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        return await DbSet
            .AnyAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> GetByFavoriteProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        if (providerId == Guid.Empty)
            throw new ArgumentException("ProviderId cannot be empty", nameof(providerId));

        return await DbSet
            .Include(c => c.FavoriteProviders)
            .Where(c => c.FavoriteProviders.Any(fp => fp.ProviderId == providerId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetFavoriteProviderIdsAsync(CustomerId customerId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customerId);

        var customer = await DbSet
            .Include(c => c.FavoriteProviders)
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        if (customer == null)
            return new List<Guid>();

        return customer.FavoriteProviders
            .Select(fp => fp.ProviderId)
            .ToList();
    }

    public async Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.FavoriteProviders)
            .ToListAsync(cancellationToken);
    }

    // Enhanced GetByIdAsync with includes for domain operations
    public override async Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.FavoriteProviders)
            .Include(c=>c.NotificationPreferences)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    // Save method with proper entity tracking
    public override async Task SaveAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var entry = Context.Entry(customer);

        if (entry.State == EntityState.Detached)
        {
            await DbSet.AddAsync(customer, cancellationToken);
        }
        else
        {
            DbSet.Update(customer);
        }
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);

        await Task.FromResult(DbSet.Update(customer));
    }
}

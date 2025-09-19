// 📁 Booksy.UserManagement.Infrastructure/Persistence/Repositories/UserRepository.cs - FIXED
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Booksy.UserManagement.Infrastructure.Persistence.Repositories;

public class UserRepository : EfRepositoryBase<User, UserId, UserManagementDbContext>, IUserRepository
{
    public UserRepository(UserManagementDbContext context) : base(context) { }

    // ✅ Domain-specific methods
    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        return await DbSet
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        return await DbSet
            .AnyAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    // ✅ Enhanced GetByIdAsync with includes for domain operations
    public override async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    // ✅ Business logic in save
    public override async Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var entry = Context.Entry(user);

        if (entry.State == EntityState.Detached)
        {
            // Check for duplicate email before adding
            if (await ExistsByEmailAsync(user.Email, cancellationToken))
            {
                throw new InvalidOperationException($"User with email {user.Email.Value} already exists");
            }

            await DbSet.AddAsync(user, cancellationToken);
        }
        else
        {
            DbSet.Update(user);
        }
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        await Task.FromResult(DbSet.Update(user));
    }

    public Task<List<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
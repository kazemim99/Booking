// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Repositories/UserWriteRepository.cs
// ========================================
using Microsoft.EntityFrameworkCore;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Infrastructure.Persistence.Repositories
{
    public class UserWriteRepository : IUserWriteRepository
    {
        private readonly UserManagementDbContext _context;

        public UserWriteRepository(UserManagementDbContext context)
        {
            _context = context;
        }


        public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.Profile)
                .Include(u => u.Roles)
                .Include(u => u.RefreshTokens)
                .Include(u => u.ActiveSessions)
                .Include(u => u.RecentLoginAttempts)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.Profile)
                .Include(u => u.Roles)
                .Include(u => u.RefreshTokens)
                .Include(u => u.ActiveSessions)
                .Include(u => u.RecentLoginAttempts)
                .FirstOrDefaultAsync(c=>c.Email.Value == email.Value, cancellationToken);
        }


        //public IUnitOfWork UnitOfWork => _context;

        public async Task<User> AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            var entry = await _context.Users.AddAsync(entity, cancellationToken);
            return entry.Entity;
        }

        public async Task<User> AddUserAsync(User user, CancellationToken cancellationToken = default)
        {
            return await AddAsync(user, cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<User> entities, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddRangeAsync(entities, cancellationToken);
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _context.Update(user);
            await Task.CompletedTask;
        }


        public Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
        {
            return UpdateAsync(user, cancellationToken);
        }

        public Task UpdateRangeAsync(IEnumerable<User> entities, CancellationToken cancellationToken = default)
        {
            _context.Users.UpdateRange(entities);
            return Task.CompletedTask;
        }

        public Task BulkUpdateAsync(IEnumerable<User> users, CancellationToken cancellationToken = default)
        {
            return UpdateRangeAsync(users, cancellationToken);
        }

        public Task RemoveAsync(User entity, CancellationToken cancellationToken = default)
        {
            // Soft delete by setting status
            entity.Deactivate("Deleted");
            _context.Users.Update(entity);
            return Task.CompletedTask;
        }

        public async Task RemoveByIdAsync(UserId id, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
            if (user != null)
            {
                await RemoveAsync(user, cancellationToken);
            }
        }

        public async Task DeleteUserAsync(UserId userId, CancellationToken cancellationToken = default)
        {
            await RemoveByIdAsync(userId, cancellationToken);
        }

        public Task RemoveRangeAsync(IEnumerable<User> entities, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
            {
                entity.Deactivate("Bulk deleted");
            }
            _context.Users.UpdateRange(entities);
            return Task.CompletedTask;
        }

        public void Attach(User entity)
        {
            _context.Users.Attach(entity);
        }

        public void Detach(User entity)
        {
            _context.Entry(entity).State = EntityState.Detached;
        }
    }
}


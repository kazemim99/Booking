// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Repositories/UserWriteRepository.cs
// ========================================
using Microsoft.EntityFrameworkCore;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Enums;
using System.Linq.Expressions;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Booksy.Core.Application.DTOs;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Infrastructure.Persistence.Repositories
{
    public class UserReadRepository : IUserReadRepository
    {
        private readonly UserManagementDbContext _context;
        private readonly IQueryable<User> _queryable;

        public UserReadRepository(UserManagementDbContext context)
        {
            _context = context;
            _queryable = _context.Users
                .Include(u => u.Profile)
                .Include(u => u.Roles)
                .Include(u => u.RefreshTokens)
                .Include(u => u.ActiveSessions)
                .AsSplitQuery()
                .AsNoTracking();
        }

        public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        {
            return await _queryable
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            var emailValue = email.Value;
            return await _queryable
                .FirstOrDefaultAsync(u => u.Email.Value == emailValue, cancellationToken);
        }

        public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            var emailValue = email.Value;
            return await _context.Users
                .AnyAsync(u => u.Email.Value == emailValue, cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _queryable.ToListAsync(cancellationToken);
        }

    

        public async Task<IReadOnlyList<User>> GetByTypeAsync(UserType type, CancellationToken cancellationToken = default)
        {
            return await _queryable
                .Where(u => u.Type == type)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetByRoleAsync(string roleName, CancellationToken cancellationToken = default)
        {
            return await _queryable
                .Where(u => u.Roles.Any(r => r.Name == roleName))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetRegisteredBetweenAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await _queryable
                .Where(u => u.RegisteredAt >= startDate && u.RegisteredAt <= endDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> FindAsync(
            Expression<Func<User, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await _queryable
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        public async Task<User?> FindSingleAsync(
            Expression<Func<User, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await _queryable
                .FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public async Task<bool> ExistsAsync(
            Expression<Func<User, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await _queryable.AnyAsync(predicate, cancellationToken);
        }

        public async Task<int> CountAsync(
            Expression<Func<User, bool>>? predicate = null,
            CancellationToken cancellationToken = default)
        {
            return predicate == null
                ? await _queryable.CountAsync(cancellationToken)
                : await _queryable.CountAsync(predicate, cancellationToken);
        }

        public IQueryable<User> GetQueryable()
        {
            return _queryable;
        }

        public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<User, bool>>? predicate = null,
            Expression<Func<User, object>>? orderBy = null,
            bool descending = false,
            CancellationToken cancellationToken = default)
        {
            var query = predicate == null ? _queryable : _queryable.Where(predicate);

            var totalCount = await query.CountAsync(cancellationToken);

            if (orderBy != null)
            {
                query = descending
                    ? query.OrderByDescending(orderBy)
                    : query.OrderBy(orderBy);
            }
            else
            {
                query = query.OrderByDescending(u => u.RegisteredAt);
            }

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<(IReadOnlyList<User> Users, int TotalCount)> GetPaginatedAsync(
            int pageNumber,
            int pageSize,
            UserStatus? status = null,
            UserType? type = null,
            CancellationToken cancellationToken = default)
        {
            var query = _queryable;

            if (status.HasValue)
                query = query.Where(u => u.Status == status.Value);

            if (type.HasValue)
                query = query.Where(u => u.Type == type.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderByDescending(u => u.RegisteredAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (users, totalCount);
        }

        public async Task<IReadOnlyList<User>> GetWithIncludesAsync(
            Expression<Func<User, bool>>? predicate = null,
            params Expression<Func<User, object>>[] includes)
        {
            var query = _context.Users.AsQueryable();

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.AsNoTracking().ToListAsync();
        }

        public async Task<IQueryable<User>> SearchAsync(
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            var term = searchTerm.ToLower();

            return _queryable
                .Where(u =>
                    u.Email.Value.ToLower().Contains(term) ||
                    u.Profile.FirstName.ToLower().Contains(term) ||
                    u.Profile.LastName.ToLower().Contains(term) ||
                    (u.Profile.MiddleName != null && u.Profile.MiddleName.ToLower().Contains(term)));
            
        }

        public async Task<IReadOnlyList<User>> GetUsersWithExpiredActivationTokensAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _queryable
                .Where(u => u.Status == UserStatus.Pending &&
                           u.ActivationToken != null &&
                           u.ActivationToken.ExpiresAt < now)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetExpiredLockedAccountsAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _queryable
                .Where(u => u.LockedUntil != null && u.LockedUntil < now)
                .ToListAsync(cancellationToken);
        }

        public async Task<UserStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekAgo = today.AddDays(-7);
            var monthAgo = today.AddMonths(-1);

            var totalUsers = await _context.Users.CountAsync(cancellationToken);
            var activeUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Active, cancellationToken);
            var pendingUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Pending, cancellationToken);
            var suspendedUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Suspended, cancellationToken);
            var deletedUsers = await _context.Users.IgnoreQueryFilters()
                .CountAsync(u => u.Status == UserStatus.Deleted, cancellationToken);

            var usersByType = await _context.Users
                .GroupBy(u => u.Type)
                .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken);

            var usersByRole = await _context.UserRoles
                .GroupBy(ur => ur.Name)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Role, x => x.Count, cancellationToken);

            var registeredToday = await _context.Users
                .CountAsync(u => u.RegisteredAt >= today, cancellationToken);

            var registeredThisWeek = await _context.Users
                .CountAsync(u => u.RegisteredAt >= weekAgo, cancellationToken);

            var registeredThisMonth = await _context.Users
                .CountAsync(u => u.RegisteredAt >= monthAgo, cancellationToken);

            return new UserStatistics
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                PendingUsers = pendingUsers,
                SuspendedUsers = suspendedUsers,
                DeletedUsers = deletedUsers,
                UsersByType = usersByType,
                UsersByRole = usersByRole,
                UsersRegisteredToday = registeredToday,
                UsersRegisteredThisWeek = registeredThisWeek,
                UsersRegisteredThisMonth = registeredThisMonth
            };
        }

     
    }
}


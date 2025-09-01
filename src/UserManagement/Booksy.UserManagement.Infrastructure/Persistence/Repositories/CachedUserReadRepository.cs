
// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Repositories/CachedUserReadRepository.cs
// ========================================
using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Decorator for UserReadRepository that adds caching
    /// </summary>
    public class CachedUserReadRepository : IUserReadRepository
    {
        private readonly IUserReadRepository _innerRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CachedUserReadRepository> _logger;
        private readonly TimeSpan _defaultCacheDuration = TimeSpan.FromMinutes(5);

        public CachedUserReadRepository(
            IUserReadRepository innerRepository,
            IDistributedCache cache,
            ILogger<CachedUserReadRepository> logger)
        {
            _innerRepository = innerRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"user:id:{id.Value}";

            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogDebug("Cache hit for user ID: {UserId}", id.Value);
                    return JsonSerializer.Deserialize<User>(cachedData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading from cache for user ID: {UserId}", id.Value);
            }

            var user = await _innerRepository.GetByIdAsync(id, cancellationToken);

            if (user != null)
            {
                await SetCacheAsync(cacheKey, user, cancellationToken);
            }

            return user;
        }

        public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"user:email:{email.Value}";

            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogDebug("Cache hit for email: {Email}", email.Value);
                    return JsonSerializer.Deserialize<User>(cachedData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading from cache for email: {Email}", email.Value);
            }

            var user = await _innerRepository.GetByEmailAsync(email, cancellationToken);

            if (user != null)
            {
                await SetCacheAsync(cacheKey, user, cancellationToken);
            }

            return user;
        }

        public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"user:exists:email:{email.Value}";

            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogDebug("Cache hit for email existence: {Email}", email.Value);
                    return bool.Parse(cachedData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading from cache for email existence: {Email}", email.Value);
            }

            var exists = await _innerRepository.ExistsByEmailAsync(email, cancellationToken);

            await SetCacheAsync(cacheKey, exists.ToString(), TimeSpan.FromMinutes(1), cancellationToken);

            return exists;
        }

        // Delegate non-cached methods to inner repository
        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
            => _innerRepository.GetAllAsync(cancellationToken);

        public Task<IReadOnlyList<User>> GetByTypeAsync(UserType type, CancellationToken cancellationToken = default)
            => _innerRepository.GetByTypeAsync(type, cancellationToken);

        public Task<IReadOnlyList<User>> GetByRoleAsync(string roleName, CancellationToken cancellationToken = default)
            => _innerRepository.GetByRoleAsync(roleName, cancellationToken);

        public Task<IReadOnlyList<User>> GetRegisteredBetweenAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
            => _innerRepository.GetRegisteredBetweenAsync(startDate, endDate, cancellationToken);

        public Task<IReadOnlyList<User>> FindAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default)
            => _innerRepository.FindAsync(predicate, cancellationToken);

        public Task<User?> FindSingleAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default)
            => _innerRepository.FindSingleAsync(predicate, cancellationToken);

        public Task<bool> ExistsAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default)
            => _innerRepository.ExistsAsync(predicate, cancellationToken);

        public Task<int> CountAsync(Expression<Func<User, bool>>? predicate = null, CancellationToken cancellationToken = default)
            => _innerRepository.CountAsync(predicate, cancellationToken);

        public IQueryable<User> GetQueryable()
            => _innerRepository.GetQueryable();

        //public Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        //    int pageNumber,
        //    int pageSize,
        //    Expression<Func<User, bool>>? predicate = null,
        //    Expression<Func<User, object>>? orderBy = null,
        //    bool descending = false,
        //    CancellationToken cancellationToken = default)
        //    => _innerRepository.GetPagedAsync(pageNumber, pageSize, predicate, orderBy, descending, cancellationToken);

        //public Task<(IReadOnlyList<User> Users, int TotalCount)> GetPaginatedAsync(
        //    int pageNumber,
        //    int pageSize,
        //    UserStatus? status = null,
        //    UserType? type = null,
        //    CancellationToken cancellationToken = default)
        //    => _innerRepository.GetPaginatedAsync(pageNumber, pageSize, status, type, cancellationToken);

        public Task<IReadOnlyList<User>> GetWithIncludesAsync(
            Expression<Func<User, bool>>? predicate = null,
            params Expression<Func<User, object>>[] includes)
            => _innerRepository.GetWithIncludesAsync(predicate, includes);

        public Task<IQueryable<User>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
            => _innerRepository.SearchAsync(searchTerm, cancellationToken);

        public Task<IReadOnlyList<User>> GetUsersWithExpiredActivationTokensAsync(CancellationToken cancellationToken = default)
            => _innerRepository.GetUsersWithExpiredActivationTokensAsync(cancellationToken);

        public Task<IReadOnlyList<User>> GetExpiredLockedAccountsAsync(CancellationToken cancellationToken = default)
            => _innerRepository.GetExpiredLockedAccountsAsync(cancellationToken);

        public async Task<UserStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var cacheKey = "user:statistics";

            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogDebug("Cache hit for user statistics");
                    return JsonSerializer.Deserialize<UserStatistics>(cachedData)!;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading from cache for user statistics");
            }

            var statistics = await _innerRepository.GetStatisticsAsync(cancellationToken);

            await SetCacheAsync(cacheKey, statistics, TimeSpan.FromMinutes(10), cancellationToken);

            return statistics;
        }

        private async Task SetCacheAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        {
            await SetCacheAsync(key, value, _defaultCacheDuration, cancellationToken);
        }

        private async Task SetCacheAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    SlidingExpiration = duration
                };

                var serialized = typeof(T) == typeof(string)
                    ? value as string
                    : JsonSerializer.Serialize(value);

                await _cache.SetStringAsync(key, serialized!, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error writing to cache for key: {Key}", key);
            }
        }

     
    }
}
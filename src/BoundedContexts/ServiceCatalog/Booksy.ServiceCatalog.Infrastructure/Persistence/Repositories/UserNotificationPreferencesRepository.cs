// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/UserNotificationPreferencesRepository.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository for UserNotificationPreferences aggregate
    /// </summary>
    public sealed class UserNotificationPreferencesRepository
        : EfWriteRepositoryBase<UserNotificationPreferences, UserId, ServiceCatalogDbContext>,
          IUserNotificationPreferencesRepository
    {
        private readonly ILogger<UserNotificationPreferencesRepository> _logger;

        public UserNotificationPreferencesRepository(
            ServiceCatalogDbContext context,
            ILogger<UserNotificationPreferencesRepository> logger)
            : base(context)
        {
            _logger = logger;
        }

        public async Task<UserNotificationPreferences?> GetByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);
        }

        public async Task<UserNotificationPreferences> SaveAsync(
            UserNotificationPreferences preferences,
            CancellationToken cancellationToken = default)
        {
            await base.SaveAsync(preferences, cancellationToken);
            _logger.LogInformation(
                "Saved notification preferences for user: {UserId}",
                preferences.Id.Value);
            return preferences;
        }

        public async Task UpdateAsync(
            UserNotificationPreferences preferences,
            CancellationToken cancellationToken = default)
        {
            await base.UpdateAsync(preferences, cancellationToken);
            _logger.LogDebug(
                "Updated notification preferences for user: {UserId}",
                preferences.Id.Value);
        }

        public async Task DeleteAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            var preferences = await GetByUserIdAsync(userId, cancellationToken);
            if (preferences != null)
            {
                await DeleteAsync(preferences.Id, cancellationToken);
                _logger.LogInformation(
                    "Deleted notification preferences for user: {UserId}",
                    userId.Value);
            }
        }

        public async Task<bool> ExistsAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet.AnyAsync(p => p.Id == userId, cancellationToken);
        }
    }
}

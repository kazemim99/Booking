// ========================================
// AsanRezerve.ServiceCatalog.Domain/Repositories/IUserNotificationPreferencesRepository.cs
// ========================================
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;

namespace AsanRezerve.ServiceCatalog.Domain.Repositories
{
    /// <summary>
    /// Repository for managing user notification preferences
    /// </summary>
    public interface IUserNotificationPreferencesRepository
    {
        /// <summary>
        /// Get user notification preferences by user ID
        /// </summary>
        Task<UserNotificationPreferences?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Save new user notification preferences
        /// </summary>
        Task<UserNotificationPreferences> SaveAsync(UserNotificationPreferences preferences, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update existing user notification preferences
        /// </summary>
        Task UpdateAsync(UserNotificationPreferences preferences, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete user notification preferences
        /// </summary>
        Task DeleteAsync(UserId userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if user has custom preferences (vs default)
        /// </summary>
        Task<bool> ExistsAsync(UserId userId, CancellationToken cancellationToken = default);
    }
}

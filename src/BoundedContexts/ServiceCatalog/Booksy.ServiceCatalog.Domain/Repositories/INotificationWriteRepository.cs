// ========================================
// Booksy.ServiceCatalog.Domain/Repositories/INotificationWriteRepository.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    /// <summary>
    /// Write repository for Notification aggregate - optimized for commands
    /// </summary>
    public interface INotificationWriteRepository : IWriteRepository<Notification, NotificationId>
    {
        /// <summary>
        /// Get notification by ID for updates (with tracking)
        /// </summary>
        Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Save a new notification
        /// </summary>
        Task SaveNotificationAsync(Notification notification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update an existing notification
        /// </summary>
        Task UpdateNotificationAsync(Notification notification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get notifications that need to be sent (queued or failed with retry)
        /// </summary>
        Task<List<Notification>> GetPendingNotificationsAsync(int batchSize = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get scheduled notifications that are ready to send
        /// </summary>
        Task<List<Notification>> GetScheduledNotificationsToSendAsync(DateTime currentTime, int batchSize = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get expired notifications to clean up
        /// </summary>
        Task<List<Notification>> GetExpiredNotificationsAsync(DateTime currentTime, int batchSize = 100, CancellationToken cancellationToken = default);
    }
}

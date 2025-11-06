// ========================================
// Booksy.ServiceCatalog.Domain/Repositories/INotificationReadRepository.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    /// <summary>
    /// Read repository for Notification aggregate - optimized for queries
    /// </summary>
    public interface INotificationReadRepository : IReadRepository<Notification, NotificationId>
    {
        /// <summary>
        /// Get notification by ID (no tracking)
        /// </summary>
        Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get user notification history with filtering and pagination
        /// </summary>
        Task<(List<Notification> Notifications, int TotalCount)> GetUserNotificationHistoryAsync(
            UserId userId,
            NotificationChannel? channel = null,
            NotificationType? type = null,
            NotificationStatus? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get notification analytics for a user
        /// </summary>
        Task<Dictionary<string, int>> GetUserNotificationAnalyticsAsync(
            UserId userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get notification count by status for a user
        /// </summary>
        Task<Dictionary<NotificationStatus, int>> GetNotificationCountByStatusAsync(
            UserId userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get recent notifications for a user
        /// </summary>
        Task<List<Notification>> GetRecentNotificationsAsync(
            UserId userId,
            int count = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get notification delivery statistics
        /// </summary>
        Task<(int TotalSent, int Delivered, int Failed, double DeliveryRate)> GetDeliveryStatisticsAsync(
            UserId? userId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);
    }
}

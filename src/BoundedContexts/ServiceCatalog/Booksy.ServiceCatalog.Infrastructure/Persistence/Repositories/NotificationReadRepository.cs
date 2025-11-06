// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/NotificationReadRepository.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Read repository for Notification aggregate - optimized for queries
    /// </summary>
    public sealed class NotificationReadRepository
        : EfReadRepositoryBase<Notification, NotificationId, ServiceCatalogDbContext>,
          INotificationReadRepository
    {
        private readonly ILogger<NotificationReadRepository> _logger;

        public NotificationReadRepository(
            ServiceCatalogDbContext context,
            ILogger<NotificationReadRepository> logger)
            : base(context)
        {
            _logger = logger;
        }

        public async Task<Notification?> GetByIdAsync(
            NotificationId id,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        }

        public async Task<(List<Notification> Notifications, int TotalCount)> GetUserNotificationHistoryAsync(
            UserId userId,
            NotificationChannel? channel = null,
            NotificationType? type = null,
            NotificationStatus? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var query = DbSet
                .AsNoTracking()
                .Where(n => n.RecipientId == userId);

            // Apply filters
            if (channel.HasValue)
                query = query.Where(n => n.Channel == channel.Value);

            if (type.HasValue)
                query = query.Where(n => n.Type == type.Value);

            if (status.HasValue)
                query = query.Where(n => n.Status == status.Value);

            if (startDate.HasValue)
                query = query.Where(n => n.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(n => n.CreatedAt <= endDate.Value);

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination and ordering
            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (notifications, totalCount);
        }

        public async Task<Dictionary<string, int>> GetUserNotificationAnalyticsAsync(
            UserId userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = DbSet
                .AsNoTracking()
                .Where(n => n.RecipientId == userId);

            if (startDate.HasValue)
                query = query.Where(n => n.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(n => n.CreatedAt <= endDate.Value);

            var analytics = new Dictionary<string, int>
            {
                ["TotalNotifications"] = await query.CountAsync(cancellationToken),
                ["SentNotifications"] = await query.CountAsync(n => n.Status == NotificationStatus.Sent || n.Status == NotificationStatus.Delivered, cancellationToken),
                ["DeliveredNotifications"] = await query.CountAsync(n => n.Status == NotificationStatus.Delivered, cancellationToken),
                ["FailedNotifications"] = await query.CountAsync(n => n.Status == NotificationStatus.Failed, cancellationToken),
                ["PendingNotifications"] = await query.CountAsync(n => n.Status == NotificationStatus.Pending || n.Status == NotificationStatus.Queued, cancellationToken),
                ["EmailNotifications"] = await query.CountAsync(n => n.Channel == NotificationChannel.Email, cancellationToken),
                ["SmsNotifications"] = await query.CountAsync(n => n.Channel == NotificationChannel.SMS, cancellationToken),
                ["PushNotifications"] = await query.CountAsync(n => n.Channel == NotificationChannel.PushNotification, cancellationToken),
                ["InAppNotifications"] = await query.CountAsync(n => n.Channel == NotificationChannel.InApp, cancellationToken)
            };

            return analytics;
        }

        public async Task<Dictionary<NotificationStatus, int>> GetNotificationCountByStatusAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            var counts = await DbSet
                .AsNoTracking()
                .Where(n => n.RecipientId == userId)
                .GroupBy(n => n.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

            return counts;
        }

        public async Task<List<Notification>> GetRecentNotificationsAsync(
            UserId userId,
            int count = 10,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<(int TotalSent, int Delivered, int Failed, double DeliveryRate)> GetDeliveryStatisticsAsync(
            UserId? userId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = DbSet.AsNoTracking();

            if (userId.HasValue)
                query = query.Where(n => n.RecipientId == userId.Value);

            if (startDate.HasValue)
                query = query.Where(n => n.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(n => n.CreatedAt <= endDate.Value);

            var sentQuery = query.Where(n => n.Status == NotificationStatus.Sent || n.Status == NotificationStatus.Delivered);
            var totalSent = await sentQuery.CountAsync(cancellationToken);
            var delivered = await query.CountAsync(n => n.Status == NotificationStatus.Delivered, cancellationToken);
            var failed = await query.CountAsync(n => n.Status == NotificationStatus.Failed, cancellationToken);

            var deliveryRate = totalSent > 0 ? (double)delivered / totalSent * 100 : 0;

            return (totalSent, delivered, failed, deliveryRate);
        }
    }
}

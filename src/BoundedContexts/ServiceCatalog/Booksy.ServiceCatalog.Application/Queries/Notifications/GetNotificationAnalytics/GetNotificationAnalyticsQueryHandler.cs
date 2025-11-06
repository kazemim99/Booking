// ========================================
// Booksy.ServiceCatalog.Application/Queries/Notifications/GetNotificationAnalytics/GetNotificationAnalyticsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Notifications.GetNotificationAnalytics
{
    /// <summary>
    /// Handler for GetNotificationAnalyticsQuery
    /// </summary>
    public sealed class GetNotificationAnalyticsQueryHandler
        : IQueryHandler<GetNotificationAnalyticsQuery, NotificationAnalyticsViewModel>
    {
        private readonly INotificationReadRepository _notificationRepository;
        private readonly ILogger<GetNotificationAnalyticsQueryHandler> _logger;

        public GetNotificationAnalyticsQueryHandler(
            INotificationReadRepository notificationRepository,
            ILogger<GetNotificationAnalyticsQueryHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task<NotificationAnalyticsViewModel> Handle(
            GetNotificationAnalyticsQuery request,
            CancellationToken cancellationToken)
        {
            var userId = UserId.From(request.UserId);

            var analytics = await _notificationRepository.GetUserNotificationAnalyticsAsync(
                userId,
                request.StartDate,
                request.EndDate,
                cancellationToken);

            var statusCounts = await _notificationRepository.GetNotificationCountByStatusAsync(
                userId,
                cancellationToken);

            var (totalSent, delivered, failed, deliveryRate) = await _notificationRepository.GetDeliveryStatisticsAsync(
                userId,
                request.StartDate,
                request.EndDate,
                cancellationToken);

            var notificationsByChannel = new Dictionary<string, int>
            {
                ["Email"] = analytics.GetValueOrDefault("EmailNotifications", 0),
                ["SMS"] = analytics.GetValueOrDefault("SmsNotifications", 0),
                ["PushNotification"] = analytics.GetValueOrDefault("PushNotifications", 0),
                ["InApp"] = analytics.GetValueOrDefault("InAppNotifications", 0)
            };

            var notificationsByStatus = statusCounts.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value);

            // Note: NotificationsByType would require additional data aggregation
            // For now, we'll return an empty dictionary or implement later
            var notificationsByType = new Dictionary<string, int>();

            _logger.LogInformation(
                "Retrieved analytics for user {UserId}: Total={Total}, Delivered={Delivered}, Failed={Failed}, Rate={Rate:F2}%",
                request.UserId,
                analytics.GetValueOrDefault("TotalNotifications", 0),
                delivered,
                failed,
                deliveryRate);

            return new NotificationAnalyticsViewModel(
                request.UserId,
                request.StartDate,
                request.EndDate,
                analytics.GetValueOrDefault("TotalNotifications", 0),
                analytics.GetValueOrDefault("SentNotifications", 0),
                analytics.GetValueOrDefault("DeliveredNotifications", 0),
                analytics.GetValueOrDefault("FailedNotifications", 0),
                analytics.GetValueOrDefault("PendingNotifications", 0),
                deliveryRate,
                notificationsByChannel,
                notificationsByType,
                notificationsByStatus);
        }
    }
}

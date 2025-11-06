// ========================================
// Booksy.ServiceCatalog.Application/Queries/Notifications/GetDeliveryStatus/GetDeliveryStatusQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Notifications.GetDeliveryStatus
{
    /// <summary>
    /// Handler for GetDeliveryStatusQuery
    /// </summary>
    public sealed class GetDeliveryStatusQueryHandler
        : IQueryHandler<GetDeliveryStatusQuery, DeliveryStatusViewModel?>
    {
        private readonly INotificationReadRepository _notificationRepository;
        private readonly ILogger<GetDeliveryStatusQueryHandler> _logger;

        public GetDeliveryStatusQueryHandler(
            INotificationReadRepository notificationRepository,
            ILogger<GetDeliveryStatusQueryHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task<DeliveryStatusViewModel?> Handle(
            GetDeliveryStatusQuery request,
            CancellationToken cancellationToken)
        {
            var notificationId = NotificationId.From(request.NotificationId);
            var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken);

            if (notification == null)
            {
                _logger.LogWarning("Notification not found: {NotificationId}", request.NotificationId);
                return null;
            }

            var deliveryAttempts = notification.DeliveryAttempts
                .Select(a => new DeliveryAttemptDto(
                    a.AttemptedAt,
                    a.Status.ToString(),
                    a.ErrorMessage,
                    a.GatewayMessageId,
                    null)) // ResponseTime is not available
                .ToList();

            var lastAttempt = notification.DeliveryAttempts.LastOrDefault();
            var failedAttempt = notification.DeliveryAttempts.FirstOrDefault(a => a.Status == Domain.Enums.NotificationStatus.Failed);

            return new DeliveryStatusViewModel(
                notification.Id.Value,
                notification.Status.ToString(),
                notification.Channel.ToString(),
                notification.Type.ToString(),
                notification.Priority.ToString(),
                notification.CreatedAt,
                notification.CreatedAt, // QueuedAt - using CreatedAt as fallback
                notification.ScheduledFor,
                notification.SentAt,
                notification.DeliveredAt,
                failedAttempt?.AttemptedAt, // FailedAt - using first failed attempt time
                notification.ErrorMessage, // FailureReason
                notification.GatewayMessageId, // ExternalMessageId
                notification.AttemptCount,
                5, // MaxRetryAttempts - hardcoded based on DeliveryAttempt logic
                notification.DeliveryAttempts.Any(a => a.ShouldRetry()), // ShouldRetry
                notification.IsExpired(),
                notification.ExpiresAt,
                lastAttempt?.AttemptedAt, // LastAttemptAt
                lastAttempt?.NextRetryAt, // NextRetryAt
                deliveryAttempts);
        }
    }
}

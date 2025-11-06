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
                    a.Status,
                    a.ErrorMessage,
                    a.ExternalMessageId,
                    a.ResponseTime))
                .ToList();

            return new DeliveryStatusViewModel(
                notification.Id.Value,
                notification.Status.ToString(),
                notification.Channel.ToString(),
                notification.Type.ToString(),
                notification.Priority.ToString(),
                notification.CreatedAt,
                notification.QueuedAt,
                notification.ScheduledFor,
                notification.SentAt,
                notification.DeliveredAt,
                notification.FailedAt,
                notification.FailureReason,
                notification.ExternalMessageId,
                notification.AttemptCount,
                notification.MaxRetryAttempts,
                notification.ShouldRetry(),
                notification.IsExpired(),
                notification.ExpiresAt,
                notification.LastAttemptAt,
                notification.NextRetryAt,
                deliveryAttempts);
        }
    }
}

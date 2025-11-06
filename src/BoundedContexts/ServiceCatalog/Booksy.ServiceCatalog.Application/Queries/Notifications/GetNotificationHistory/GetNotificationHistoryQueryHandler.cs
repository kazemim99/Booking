// ========================================
// Booksy.ServiceCatalog.Application/Queries/Notifications/GetNotificationHistory/GetNotificationHistoryQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Notifications.GetNotificationHistory
{
    /// <summary>
    /// Handler for GetNotificationHistoryQuery
    /// </summary>
    public sealed class GetNotificationHistoryQueryHandler
        : IQueryHandler<GetNotificationHistoryQuery, NotificationHistoryViewModel>
    {
        private readonly INotificationReadRepository _notificationRepository;
        private readonly ILogger<GetNotificationHistoryQueryHandler> _logger;

        public GetNotificationHistoryQueryHandler(
            INotificationReadRepository notificationRepository,
            ILogger<GetNotificationHistoryQueryHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task<NotificationHistoryViewModel> Handle(
            GetNotificationHistoryQuery request,
            CancellationToken cancellationToken)
        {
            var userId = UserId.From(request.UserId);

            var (notifications, totalCount) = await _notificationRepository.GetUserNotificationHistoryAsync(
                userId,
                request.Channel,
                request.Type,
                request.Status,
                request.StartDate,
                request.EndDate,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var items = notifications.Select(n => new NotificationHistoryItem(
                n.Id.Value,
                n.Type.ToString(),
                n.Channel.ToString(),
                n.Status.ToString(),
                n.Subject,
                n.Body,
                n.Priority.ToString(),
                n.RecipientEmail,
                n.RecipientPhone,
                n.CreatedAt,
                n.SentAt,
                n.DeliveredAt,
                n.DeliveryAttempts.FirstOrDefault(a => a.Status == Domain.Enums.NotificationStatus.Failed)?.AttemptedAt,
                n.ErrorMessage,
                n.GatewayMessageId,
                n.AttemptCount,
                n.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString() ?? "")))
            .ToList();

            _logger.LogInformation(
                "Retrieved notification history for user {UserId}: {Count} notifications (Page {PageNumber}/{TotalPages})",
                request.UserId,
                totalCount,
                request.PageNumber,
                totalPages);

            return new NotificationHistoryViewModel(
                totalCount,
                request.PageNumber,
                request.PageSize,
                totalPages,
                items);
        }
    }
}

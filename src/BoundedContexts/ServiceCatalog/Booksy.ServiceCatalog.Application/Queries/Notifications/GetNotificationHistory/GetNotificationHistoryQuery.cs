// ========================================
// Booksy.ServiceCatalog.Application/Queries/Notifications/GetNotificationHistory/GetNotificationHistoryQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Queries.Notifications.GetNotificationHistory
{
    /// <summary>
    /// Query to get notification history for a user
    /// </summary>
    public sealed record GetNotificationHistoryQuery(
        Guid UserId,
        NotificationChannel? Channel = null,
        NotificationType? Type = null,
        NotificationStatus? Status = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        int PageNumber = 1,
        int PageSize = 20) : IQuery<NotificationHistoryViewModel>;
}

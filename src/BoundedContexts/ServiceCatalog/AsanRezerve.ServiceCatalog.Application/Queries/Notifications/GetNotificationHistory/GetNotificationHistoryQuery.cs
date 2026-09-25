// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Notifications/GetNotificationHistory/GetNotificationHistoryQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Notifications.GetNotificationHistory
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

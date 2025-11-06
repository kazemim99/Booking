// ========================================
// Booksy.ServiceCatalog.Application/Queries/Notifications/GetNotificationAnalytics/GetNotificationAnalyticsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Notifications.GetNotificationAnalytics
{
    /// <summary>
    /// Query to get notification analytics for a user
    /// </summary>
    public sealed record GetNotificationAnalyticsQuery(
        Guid UserId,
        DateTime? StartDate = null,
        DateTime? EndDate = null) : IQuery<NotificationAnalyticsViewModel>;
}

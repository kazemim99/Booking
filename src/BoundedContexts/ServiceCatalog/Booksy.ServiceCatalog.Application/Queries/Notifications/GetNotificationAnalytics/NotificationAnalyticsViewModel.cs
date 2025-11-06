// ========================================
// Booksy.ServiceCatalog.Application/Queries/Notifications/GetNotificationAnalytics/NotificationAnalyticsViewModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Notifications.GetNotificationAnalytics
{
    /// <summary>
    /// View model for notification analytics
    /// </summary>
    public sealed record NotificationAnalyticsViewModel(
        Guid UserId,
        DateTime? StartDate,
        DateTime? EndDate,
        int TotalNotifications,
        int SentNotifications,
        int DeliveredNotifications,
        int FailedNotifications,
        int PendingNotifications,
        double DeliveryRate,
        Dictionary<string, int> NotificationsByChannel,
        Dictionary<string, int> NotificationsByType,
        Dictionary<string, int> NotificationsByStatus)
    {
    }
}

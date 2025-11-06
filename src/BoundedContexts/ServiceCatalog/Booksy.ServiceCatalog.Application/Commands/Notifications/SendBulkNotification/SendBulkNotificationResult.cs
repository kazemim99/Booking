// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/SendBulkNotification/SendBulkNotificationResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Notifications.SendBulkNotification
{
    /// <summary>
    /// Result of sending bulk notifications
    /// </summary>
    public sealed record SendBulkNotificationResult(
        string BatchId,
        int TotalRecipients,
        int SuccessCount,
        int FailureCount,
        List<Guid> NotificationIds,
        List<string> Errors);
}

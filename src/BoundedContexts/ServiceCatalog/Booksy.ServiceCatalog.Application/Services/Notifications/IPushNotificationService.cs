// ========================================
// Booksy.ServiceCatalog.Application/Services/Notifications/IPushNotificationService.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// Service for sending push notifications (FCM/APNS)
    /// </summary>
    public interface IPushNotificationService
    {
        /// <summary>
        /// Send a push notification to a specific device token
        /// </summary>
        Task<(bool Success, string? MessageId, string? ErrorMessage)> SendPushAsync(
            string deviceToken,
            string title,
            string body,
            Dictionary<string, object>? data = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Send push notification to multiple devices
        /// </summary>
        Task<List<(string DeviceToken, bool Success, string? MessageId, string? ErrorMessage)>> SendBulkPushAsync(
            List<string> deviceTokens,
            string title,
            string body,
            Dictionary<string, object>? data = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Send push notification to a topic
        /// </summary>
        Task<(bool Success, string? MessageId, string? ErrorMessage)> SendToTopicAsync(
            string topic,
            string title,
            string body,
            Dictionary<string, object>? data = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Send push notification to a specific user by user ID
        /// </summary>
        Task<(bool Success, string? MessageId, string? ErrorMessage)> SendPushAsync(
            Guid userId,
            string title,
            string body,
            Dictionary<string, object>? data = null,
            CancellationToken cancellationToken = default);
    }
}

// ========================================
// Booksy.ServiceCatalog.Application/Services/Notifications/IPushNotificationService.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// The reasons a push cannot be delivered that are NOT delivery failures.
    /// </summary>
    /// <remarks>
    /// A recipient with no device, or an environment with no push credentials, will not improve by being
    /// retried. The dispatcher treats these as skips so they do not consume a notification's retry budget and
    /// eventually dead-letter it while its other channels were working. They live on the contract so the
    /// sender and the dispatcher cannot drift apart on the exact wording.
    /// </remarks>
    public static class PushUnavailable
    {
        public const string NotConfigured = "Push is not configured on this environment";
        public const string NoDevice = "The recipient has no registered device";
    }

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

// ========================================
// Booksy.ServiceCatalog.Infrastructure/Notifications/Push/FirebasePushNotificationService.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Notifications.Push
{
    /// <summary>
    /// Sends push notifications through Firebase Cloud Messaging.
    /// </summary>
    /// <remarks>
    /// <para><b>This class used to lie.</b> It logged a warning and returned
    /// <c>(true, Guid.NewGuid(), null)</c> without sending anything, so every push was written to the
    /// delivery log as delivered. That is worse than having no push at all: the log is the evidence support
    /// uses to answer "did the customer get it", and it was answering yes for messages that never existed.
    /// Nothing here may report success unless the gateway accepted the message.</para>
    ///
    /// <para>When FCM is not configured — no credentials on this environment — sends are reported as
    /// <i>skipped</i>, never as delivered. A skip is honest and costs nothing; the notification's other
    /// channels still go out and its retry budget is untouched.</para>
    /// </remarks>
    public sealed class FirebasePushNotificationService : IPushNotificationService
    {
        private readonly IDeviceTokenRegistry _devices;
        private readonly IFirebaseMessagingGateway _gateway;
        private readonly ILogger<FirebasePushNotificationService> _logger;

        public FirebasePushNotificationService(
            IDeviceTokenRegistry devices,
            IFirebaseMessagingGateway gateway,
            ILogger<FirebasePushNotificationService> logger)
        {
            _devices = devices;
            _gateway = gateway;
            _logger = logger;
        }

        public async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendPushAsync(
            string deviceToken,
            string title,
            string body,
            Dictionary<string, object>? data = null,
            CancellationToken cancellationToken = default)
        {
            if (!_gateway.IsConfigured)
            {
                _logger.LogDebug("Push skipped: Firebase is not configured on this environment");
                return (false, null, PushUnavailable.NotConfigured);
            }

            var result = await _gateway.SendAsync(deviceToken, title, body, Stringify(data), cancellationToken);

            if (result.TokenIsDead)
            {
                // The gateway is the only thing that can tell us a device is gone. Acting on it is what stops
                // the table filling with addresses that will never be reachable again.
                await _devices.RetireAsync(deviceToken, result.ErrorMessage ?? "rejected by gateway", cancellationToken);
            }

            return (result.Success, result.MessageId, result.ErrorMessage);
        }

        /// <summary>
        /// Sends to every live device the person has.
        /// </summary>
        /// <remarks>
        /// This is the overload the dispatcher uses: it knows a recipient, not a handset.
        ///
        /// <para>A person with no registered device is reported as a failure with <see cref="PushUnavailable.NoDevice"/>,
        /// which the dispatcher treats as a skip rather than a delivery failure — burning the retry budget on
        /// somebody who has never opened the app would eventually dead-letter a perfectly good notification.</para>
        /// </remarks>
        public async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendPushAsync(
            Guid userId,
            string title,
            string body,
            Dictionary<string, object>? data = null,
            CancellationToken cancellationToken = default)
        {
            if (!_gateway.IsConfigured)
                return (false, null, PushUnavailable.NotConfigured);

            var devices = await _devices.GetActiveForUserAsync(userId, cancellationToken);
            if (devices.Count == 0)
                return (false, null, PushUnavailable.NoDevice);

            var payload = Stringify(data);
            string? firstMessageId = null;
            string? firstError = null;
            var delivered = 0;

            foreach (var device in devices)
            {
                // One handset failing says nothing about the person's other handsets, so the loop continues
                // and the notification counts as delivered if any device took it.
                var result = await _gateway.SendAsync(device.Token, title, body, payload, cancellationToken);

                if (result.TokenIsDead)
                    await _devices.RetireAsync(device.Token, result.ErrorMessage ?? "rejected by gateway", cancellationToken);

                if (result.Success)
                {
                    delivered++;
                    firstMessageId ??= result.MessageId;
                }
                else
                {
                    firstError ??= result.ErrorMessage;
                }
            }

            if (delivered > 0)
                return (true, firstMessageId, null);

            return (false, null, firstError ?? "Push failed on every registered device");
        }

        public async Task<List<(string DeviceToken, bool Success, string? MessageId, string? ErrorMessage)>>
            SendBulkPushAsync(
                List<string> deviceTokens,
                string title,
                string body,
                Dictionary<string, object>? data = null,
                CancellationToken cancellationToken = default)
        {
            var results = new List<(string, bool, string?, string?)>(deviceTokens.Count);

            foreach (var token in deviceTokens)
            {
                var (success, messageId, error) = await SendPushAsync(token, title, body, data, cancellationToken);
                results.Add((token, success, messageId, error));
            }

            return results;
        }

        public async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendToTopicAsync(
            string topic,
            string title,
            string body,
            Dictionary<string, object>? data = null,
            CancellationToken cancellationToken = default)
        {
            if (!_gateway.IsConfigured)
                return (false, null, PushUnavailable.NotConfigured);

            var result = await _gateway.SendToTopicAsync(topic, title, body, Stringify(data), cancellationToken);
            return (result.Success, result.MessageId, result.ErrorMessage);
        }

        /// <summary>FCM data payloads are string-to-string; anything else has to be rendered first.</summary>
        private static IReadOnlyDictionary<string, string> Stringify(Dictionary<string, object>? data) =>
            data is null
                ? new Dictionary<string, string>()
                : data.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? string.Empty);
    }
}

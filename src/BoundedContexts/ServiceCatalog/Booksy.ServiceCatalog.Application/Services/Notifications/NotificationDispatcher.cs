// ========================================
// Booksy.ServiceCatalog.Application/Services/Notifications/NotificationDispatcher.cs
// ========================================
using Booksy.Core.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Enums.Extensions;
using Booksy.ServiceCatalog.Domain.Policies;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// Sends a notification on every channel it names, enforcing the delivery guarantees in one place:
    /// channel preferences, de-duplication per (event, channel, recipient), and a durable delivery log.
    /// </summary>
    public sealed class NotificationDispatcher : INotificationDispatcher
    {
        private readonly IEmailNotificationService _emailService;
        private readonly ISmsNotificationService _smsService;
        private readonly IPushNotificationService _pushService;
        private readonly IInAppNotificationService _inAppService;
        private readonly IUserNotificationPreferencesRepository _preferencesRepository;
        private readonly INotificationDeliveryLog _deliveryLog;
        private readonly ILogger<NotificationDispatcher> _logger;

        /// <summary>See <see cref="NotificationDispatchOptions.ReliableDispatch"/>.</summary>
        private readonly bool _reliableDispatch;

        public NotificationDispatcher(
            IEmailNotificationService emailService,
            ISmsNotificationService smsService,
            IPushNotificationService pushService,
            IInAppNotificationService inAppService,
            IUserNotificationPreferencesRepository preferencesRepository,
            INotificationDeliveryLog deliveryLog,
            NotificationDispatchOptions options,
            ILogger<NotificationDispatcher> logger)
        {
            _emailService = emailService;
            _smsService = smsService;
            _pushService = pushService;
            _inAppService = inAppService;
            _preferencesRepository = preferencesRepository;
            _deliveryLog = deliveryLog;
            _logger = logger;
            _reliableDispatch = options.ReliableDispatch;
        }

        public async Task<NotificationDispatchOutcome> DispatchAsync(
            Notification notification,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(notification);

            if (IsTerminal(notification.Status))
            {
                _logger.LogDebug(
                    "Notification {NotificationId} is {Status}; nothing to dispatch",
                    notification.Id.Value, notification.Status);
                return new NotificationDispatchOutcome(0, 0, 0, null, null);
            }

            if (notification.IsExpired())
            {
                // Send() would set Expired and throw. Returning instead keeps that out of the caller's face and,
                // more importantly, stops CAP redelivering an expired notification forever.
                _logger.LogWarning(
                    "Notification {NotificationId} expired at {ExpiresAt}; not dispatching",
                    notification.Id.Value, notification.ExpiresAt);
                return new NotificationDispatchOutcome(0, 0, 0, null, "Notification expired");
            }

            if (!notification.IsScheduledForNow())
            {
                // A scheduled notification that is not due yet: the aggregate would refuse the send, and the
                // sweep would keep throwing on it every cycle.
                return new NotificationDispatchOutcome(0, 0, 0, null, null);
            }

            // A failed notification only becomes sendable again once its backoff has elapsed; once the retry
            // budget is spent it is dead-lettered rather than retried forever.
            if (notification.Status == NotificationStatus.Failed)
            {
                if (notification.HasExhaustedRetries())
                {
                    notification.MarkAsDeadLettered(notification.ErrorMessage ?? "Retry budget exhausted");
                    _logger.LogError(
                        "Notification {NotificationId} dead-lettered after {Attempts} attempts: {Error}",
                        notification.Id.Value, notification.AttemptCount, notification.ErrorMessage);
                    return new NotificationDispatchOutcome(0, 0, 0, null, notification.ErrorMessage);
                }

                if (!notification.ShouldRetry())
                    return new NotificationDispatchOutcome(0, 0, 0, null, notification.ErrorMessage);

                notification.PrepareForRetry();
            }

            var preferences = _reliableDispatch
                ? await _preferencesRepository.GetByUserIdAsync(notification.RecipientId, cancellationToken)
                : null;

            var skipped = 0;
            var plan = new List<(NotificationChannel Channel, string Recipient)>();

            foreach (var channel in notification.Channel.EnumerateChannels())
            {
                var recipient = ResolveRecipient(notification, channel);

                if (recipient is null)
                {
                    // Either the channel has no transport here (WhatsApp/Telegram/…) or the recipient has no
                    // address of that kind. Neither is retryable, so this is a skip, not a delivery failure —
                    // failing would burn the retry budget and eventually dead-letter a perfectly fine notification.
                    _logger.LogWarning(
                        "Notification {NotificationId}: no usable recipient for channel {Channel}; skipping",
                        notification.Id.Value, channel);
                    skipped++;
                    continue;
                }

                if (_reliableDispatch &&
                    !NotificationSuppressionPolicy.ShouldSend(preferences, channel, notification.Type))
                {
                    _logger.LogInformation(
                        "Notification {NotificationId} ({Type}) suppressed on {Channel} by recipient {RecipientId}'s preferences",
                        notification.Id.Value, notification.Type, channel, notification.RecipientId.Value);
                    skipped++;
                    continue;
                }

                if (_reliableDispatch)
                {
                    var claim = await _deliveryLog.TryClaimAsync(
                        notification.DedupKey, channel, recipient, notification.Id.Value, cancellationToken);

                    if (claim == NotificationDeliveryClaim.AlreadyDelivered)
                    {
                        _logger.LogInformation(
                            "Notification {NotificationId}: {Channel} delivery for event {EventId} already happened; de-duplicated",
                            notification.Id.Value, channel, notification.DedupKey);
                        skipped++;
                        continue;
                    }
                }

                plan.Add((channel, recipient));
            }

            if (plan.Count == 0)
            {
                _logger.LogInformation(
                    "Notification {NotificationId}: no channels to send on ({Skipped} skipped)",
                    notification.Id.Value, skipped);
                return new NotificationDispatchOutcome(0, skipped, 0, null, null);
            }

            // Records the attempt (AttemptCount++/DeliveryAttempt) and refuses a second send of an already-sent
            // notification — the aggregate's own at-most-once guard behind the delivery log.
            notification.Send();

            var delivered = 0;
            var failed = 0;
            string? gatewayMessageId = null;
            string? firstError = null;

            foreach (var (channel, recipient) in plan)
            {
                var (success, messageId, error) = await SendOnChannelAsync(notification, channel, recipient, cancellationToken);

                if (_reliableDispatch)
                {
                    await _deliveryLog.RecordOutcomeAsync(
                        notification.DedupKey, channel, recipient, success, messageId, error, cancellationToken);
                }

                if (success)
                {
                    delivered++;
                    gatewayMessageId ??= messageId;
                    _logger.LogInformation(
                        "Notification {NotificationId} delivered on {Channel} (attempt {Attempt}, gateway message {MessageId})",
                        notification.Id.Value, channel, notification.AttemptCount, messageId);
                }
                else
                {
                    failed++;
                    firstError ??= error;
                    _logger.LogWarning(
                        "Notification {NotificationId} failed on {Channel} (attempt {Attempt}): {Error}",
                        notification.Id.Value, channel, notification.AttemptCount, error);
                }
            }

            if (failed > 0)
            {
                notification.MarkAsFailed(firstError ?? "Unknown delivery error");

                if (notification.HasExhaustedRetries())
                {
                    notification.MarkAsDeadLettered(firstError ?? "Retry budget exhausted");
                    _logger.LogError(
                        "Notification {NotificationId} dead-lettered after {Attempts} attempts: {Error}",
                        notification.Id.Value, notification.AttemptCount, firstError);
                }
            }
            else
            {
                notification.MarkAsDelivered(gatewayMessageId);
            }

            return new NotificationDispatchOutcome(delivered, skipped, failed, gatewayMessageId, firstError);
        }

        private async Task<(bool Success, string? MessageId, string? Error)> SendOnChannelAsync(
            Notification notification,
            NotificationChannel channel,
            string recipient,
            CancellationToken cancellationToken)
        {
            var metadata = notification.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            try
            {
                switch (channel)
                {
                    case NotificationChannel.Email:
                        var email = await _emailService.SendEmailAsync(
                            recipient,
                            notification.Subject,
                            notification.Body,
                            notification.PlainTextBody,
                            metadata: metadata,
                            cancellationToken: cancellationToken);
                        return (email.Success, email.MessageId, email.ErrorMessage);

                    case NotificationChannel.SMS:
                        var sms = await _smsService.SendSmsAsync(
                            recipient,
                            notification.PlainTextBody ?? notification.Body,
                            metadata,
                            cancellationToken);
                        return (sms.Success, sms.MessageId, sms.ErrorMessage);

                    case NotificationChannel.PushNotification:
                        var push = await _pushService.SendPushAsync(
                            notification.RecipientId.Value,
                            notification.Subject,
                            notification.PlainTextBody ?? notification.Body,
                            metadata,
                            cancellationToken);
                        return (push.Success, push.MessageId, push.ErrorMessage);

                    case NotificationChannel.InApp:
                        var inApp = await _inAppService.SendToUserAsync(
                            notification.RecipientId.Value,
                            notification.Subject,
                            notification.Body,
                            notification.Type.ToString(),
                            metadata,
                            cancellationToken);
                        return (inApp.Success, null, inApp.ErrorMessage);

                    default:
                        return (false, null, $"Notification channel {channel} is not supported");
                }
            }
            catch (Exception ex)
            {
                // A throwing gateway is a failed attempt, not a failed dispatch: the notification stays retryable
                // and one broken channel never aborts the others.
                _logger.LogError(ex,
                    "Notification {NotificationId}: {Channel} gateway threw", notification.Id.Value, channel);
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// The address this channel actually delivers to, or null when the notification cannot use the channel.
        /// It doubles as the recipient half of the de-duplication tuple.
        /// </summary>
        private static string? ResolveRecipient(Notification notification, NotificationChannel channel) => channel switch
        {
            NotificationChannel.Email => Blank(notification.RecipientEmail),
            NotificationChannel.SMS => Blank(notification.RecipientPhone),
            NotificationChannel.PushNotification => notification.RecipientId.Value.ToString(),
            NotificationChannel.InApp => notification.RecipientId.Value.ToString(),
            _ => null
        };

        private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

        private static bool IsTerminal(NotificationStatus status) =>
            status is NotificationStatus.Delivered
                or NotificationStatus.Read
                or NotificationStatus.Sent
                or NotificationStatus.Cancelled
                or NotificationStatus.Expired
                or NotificationStatus.DeadLettered;
    }
}

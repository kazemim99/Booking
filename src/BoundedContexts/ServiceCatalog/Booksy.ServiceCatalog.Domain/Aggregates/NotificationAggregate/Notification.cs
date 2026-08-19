// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/NotificationAggregate/Notification.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate
{
    /// <summary>
    /// Notification aggregate root representing a multi-channel notification
    /// </summary>
    public sealed class Notification : AggregateRoot<NotificationId>
    {
        private readonly List<DeliveryAttempt> _deliveryAttempts = new();

        // Identity & References
        public UserId RecipientId { get; private set; }
        public string? RecipientEmail { get; private set; }
        public string? RecipientPhone { get; private set; }
        public string? RecipientName { get; private set; }

        // Notification Content
        public NotificationType Type { get; private set; }
        public NotificationChannel Channel { get; private set; }
        public NotificationPriority Priority { get; private set; }
        public string Subject { get; private set; }
        public string Body { get; private set; }
        public string? PlainTextBody { get; private set; }
        public string? TemplateId { get; private set; }
        public Dictionary<string, string> TemplateData { get; private set; }

        // Status & Lifecycle
        public NotificationStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ScheduledFor { get; private set; }
        public DateTime? SentAt { get; private set; }
        public DateTime? DeliveredAt { get; private set; }
        public DateTime? ReadAt { get; private set; }
        public DateTime? ExpiresAt { get; private set; }

        // Delivery Tracking
        public int AttemptCount { get; private set; }
        public string? GatewayMessageId { get; private set; }
        public string? ErrorMessage { get; private set; }
        public IReadOnlyList<DeliveryAttempt> DeliveryAttempts => _deliveryAttempts.AsReadOnly();

        // Related Entities
        public BookingId? BookingId { get; private set; }
        public PaymentId? PaymentId { get; private set; }
        public ProviderId? ProviderId { get; private set; }

        // Metadata
        public Dictionary<string, object> Metadata { get; private set; }
        public string? CampaignId { get; private set; }
        public string? BatchId { get; private set; }

        /// <summary>
        /// Id of the domain/integration event that caused this notification, when there was one.
        /// </summary>
        /// <remarks>
        /// This is the stable half of the de-duplication tuple <c>(SourceEventId, Channel, Recipient)</c>. A
        /// lifecycle event that gets re-delivered (CAP redelivery, a retried handler, a replayed outbox row)
        /// carries the same event id, so the dedup log recognises the second dispatch and the customer is not
        /// notified twice. Null for notifications raised directly through the API, which fall back to
        /// <see cref="DedupKey"/>'s notification-id scope.
        /// </remarks>
        public Guid? SourceEventId { get; private set; }

        /// <summary>
        /// The de-duplication scope for this notification: the originating event when known, otherwise the
        /// notification's own id (so retries of *this* notification still cannot double-send per channel).
        /// </summary>
        public Guid DedupKey => SourceEventId ?? Id.Value;

        // Tracking
        public string? OpenedFrom { get; private set; } // IP, device info
        public string? ClickedLink { get; private set; }
        public int OpenCount { get; private set; }
        public int ClickCount { get; private set; }

        // EF Core constructor
        private Notification() : base(NotificationId.Create())
        {
            Subject = string.Empty;
            Body = string.Empty;
            TemplateData = new Dictionary<string, string>();
            Metadata = new Dictionary<string, object>();
        }

        private Notification(
            UserId recipientId,
            NotificationType type,
            NotificationChannel channel,
            string subject,
            string body,
            NotificationPriority priority = NotificationPriority.Normal,
            string? plainTextBody = null,
            DateTime? scheduledFor = null) : base(NotificationId.Create())
        {
            RecipientId = recipientId;
            Type = type;
            Channel = channel;
            Subject = subject;
            Body = body;
            PlainTextBody = plainTextBody;
            Priority = priority;
            Status = NotificationStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            ScheduledFor = scheduledFor;
            TemplateData = new Dictionary<string, string>();
            Metadata = new Dictionary<string, object>();
            AttemptCount = 0;

            // Set expiration (30 days for scheduled, 7 days for immediate)
            ExpiresAt = scheduledFor?.AddDays(30) ?? DateTime.UtcNow.AddDays(7);

            RaiseDomainEvent(new NotificationCreatedEvent(
                Id,
                RecipientId,
                Type,
                Channel,
                Priority,
                ScheduledFor));
        }

        public static Notification Create(
            UserId recipientId,
            NotificationType type,
            NotificationChannel channel,
            string subject,
            string body,
            NotificationPriority priority = NotificationPriority.Normal,
            string? plainTextBody = null,
            DateTime? scheduledFor = null)
        {
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Subject cannot be empty", nameof(subject));

            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Body cannot be empty", nameof(body));

            if (scheduledFor.HasValue && scheduledFor.Value <= DateTime.UtcNow)
                throw new ArgumentException("Scheduled time must be in the future", nameof(scheduledFor));

            return new Notification(recipientId, type, channel, subject, body, priority, plainTextBody, scheduledFor);
        }

        /// <summary>
        /// Creates an immediate notification to be sent right away
        /// </summary>
        public static Notification CreateImmediate(
            UserId recipientId,
            NotificationType type,
            NotificationChannel channel,
            string subject,
            string body,
            NotificationPriority priority = NotificationPriority.Normal,
            string? plainTextBody = null,
            string? recipientEmail = null,
            string? recipientPhone = null)
        {
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Subject cannot be empty", nameof(subject));

            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Body cannot be empty", nameof(body));

            var notification = new Notification(recipientId, type, channel, subject, body, priority, plainTextBody, scheduledFor: null);

            // Set recipient contact information
            if (!string.IsNullOrWhiteSpace(recipientEmail) || !string.IsNullOrWhiteSpace(recipientPhone))
            {
                notification.SetRecipientContact(recipientEmail, recipientPhone, null);
            }

            return notification;
        }

        /// <summary>
        /// Creates a scheduled notification to be sent at a specific time
        /// </summary>
        public static Notification Schedule(
            UserId recipientId,
            NotificationType type,
            NotificationChannel channel,
            string subject,
            string body,
            DateTime scheduledFor,
            NotificationPriority priority = NotificationPriority.Normal,
            string? plainTextBody = null,
            string? recipientEmail = null,
            string? recipientPhone = null)
        {
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Subject cannot be empty", nameof(subject));

            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Body cannot be empty", nameof(body));

            if (scheduledFor <= DateTime.UtcNow)
                throw new ArgumentException("Scheduled time must be in the future", nameof(scheduledFor));

            var notification = new Notification(recipientId, type, channel, subject, body, priority, plainTextBody, scheduledFor);

            // Set recipient contact information
            if (!string.IsNullOrWhiteSpace(recipientEmail) || !string.IsNullOrWhiteSpace(recipientPhone))
            {
                notification.SetRecipientContact(recipientEmail, recipientPhone, null);
            }

            // Set status to Queued for scheduled notifications
            notification.Status = NotificationStatus.Queued;

            return notification;
        }

        public void SetRecipientContact(string? email, string? phone, string? name)
        {
            RecipientEmail = email;
            RecipientPhone = phone;
            RecipientName = name;
        }

        public void SetTemplate(string templateId, Dictionary<string, string> templateData)
        {
            TemplateId = templateId;
            TemplateData = templateData;
        }

        public void SetRelatedEntities(BookingId? bookingId, PaymentId? paymentId, ProviderId? providerId)
        {
            BookingId = bookingId;
            PaymentId = paymentId;
            ProviderId = providerId;
        }

        public void AddMetadata(string key, string value)
        {
            Metadata[key] = value;
        }

        /// <summary>
        /// Records which event caused this notification, fixing its de-duplication scope.
        /// </summary>
        public void SetSourceEvent(Guid sourceEventId)
        {
            if (sourceEventId == Guid.Empty)
                throw new ArgumentException("Source event id cannot be empty", nameof(sourceEventId));

            SourceEventId = sourceEventId;
        }

        public void SetCampaign(string? campaignId, string? batchId)
        {
            CampaignId = campaignId;
            BatchId = batchId;
        }

        public void Queue()
        {
            EnsureValidState(() => Status == NotificationStatus.Pending, "Queue", Status.ToString());

            Status = NotificationStatus.Queued;
        }

        public void Send()
        {
            // Allow sending from Pending or Queued status
            if (Status != NotificationStatus.Pending && Status != NotificationStatus.Queued)
            {
                throw new InvalidOperationException($"Cannot send notification in {Status} status");
            }

            if (ScheduledFor.HasValue && DateTime.UtcNow < ScheduledFor.Value)
            {
                throw new InvalidOperationException("Cannot send notification before scheduled time");
            }

            if (ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value)
            {
                Status = NotificationStatus.Expired;
                throw new InvalidOperationException("Notification has expired");
            }

            AttemptCount++;
            var attempt = DeliveryAttempt.Create(AttemptCount, Channel);
            _deliveryAttempts.Add(attempt);

            Status = NotificationStatus.Sent;
            SentAt = DateTime.UtcNow;

            RaiseDomainEvent(new NotificationSentEvent(
                Id,
                RecipientId,
                Type,
                Channel,
                AttemptCount));
        }

        public void MarkAsSent(string? gatewayMessageId = null)
        {
            EnsureValidState(() => Status == NotificationStatus.Queued, "MarkAsSent", Status.ToString());

            Status = NotificationStatus.Sent;
            SentAt = DateTime.UtcNow;
            GatewayMessageId = gatewayMessageId;

            var lastAttempt = _deliveryAttempts.LastOrDefault();
            lastAttempt?.MarkAsSent(gatewayMessageId ?? string.Empty);

            RaiseDomainEvent(new NotificationSentEvent(
                Id,
                RecipientId,
                Type,
                Channel,
                AttemptCount));
        }

        public void MarkAsDelivered(string? gatewayMessageId = null)
        {
            EnsureValidState(() => Status == NotificationStatus.Sent, "MarkAsDelivered", Status.ToString());

            Status = NotificationStatus.Delivered;
            DeliveredAt = DateTime.UtcNow;
            GatewayMessageId = gatewayMessageId;

            var lastAttempt = _deliveryAttempts.LastOrDefault();
            lastAttempt?.MarkAsDelivered();

            RaiseDomainEvent(new NotificationDeliveredEvent(
                Id,
                RecipientId,
                Type,
                Channel,
                DeliveredAt.Value));
        }

        public void MarkAsRead(string? openedFrom = null)
        {
            if (Status != NotificationStatus.Delivered && Status != NotificationStatus.Read)
            {
                throw new InvalidOperationException($"Cannot mark notification as read from {Status} status");
            }

            Status = NotificationStatus.Read;
            ReadAt ??= DateTime.UtcNow;
            OpenedFrom = openedFrom;
            OpenCount++;

            var lastAttempt = _deliveryAttempts.LastOrDefault();
            lastAttempt?.MarkAsRead();
        }

        public void RecordClick(string clickedLink)
        {
            ClickedLink = clickedLink;
            ClickCount++;
        }

        public void MarkAsFailed(string errorMessage, string? errorCode = null, int? httpStatusCode = null)
        {
            Status = NotificationStatus.Failed;
            ErrorMessage = errorMessage;

            var lastAttempt = _deliveryAttempts.LastOrDefault();
            lastAttempt?.MarkAsFailed(errorMessage, errorCode, httpStatusCode);

            RaiseDomainEvent(new NotificationFailedEvent(
                Id,
                RecipientId,
                Type,
                Channel,
                errorMessage,
                AttemptCount));
        }

        public void MarkAsBounced(string reason)
        {
            Status = NotificationStatus.Bounced;
            ErrorMessage = reason;

            var lastAttempt = _deliveryAttempts.LastOrDefault();
            lastAttempt?.MarkAsBounced(reason);
        }

        public void Cancel(string? reason = null)
        {
            if (Status == NotificationStatus.Delivered || Status == NotificationStatus.Sent)
            {
                throw new InvalidOperationException("Cannot cancel a notification that has been sent or delivered");
            }

            Status = NotificationStatus.Cancelled;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                ErrorMessage = reason;
            }

            RaiseDomainEvent(new NotificationCancelledEvent(
                Id,
                RecipientId,
                Type,
                reason ?? "Cancelled by system"));
        }

        /// <summary>The maximum number of delivery attempts before a notification is dead-lettered.</summary>
        public const int MaxRetryAttempts = 5;

        public bool ShouldRetry()
        {
            if (Status != NotificationStatus.Failed)
                return false;

            if (HasExhaustedRetries())
                return false;

            var lastAttempt = _deliveryAttempts.LastOrDefault();
            return lastAttempt?.ShouldRetry() ?? false;
        }

        /// <summary>True once all delivery attempts are used up — the notification must be dead-lettered, not retried.</summary>
        public bool HasExhaustedRetries() => AttemptCount >= MaxRetryAttempts;

        /// <summary>
        /// Moves a failed-but-retryable notification back to Queued so the dispatcher can attempt delivery again.
        /// Only valid when <see cref="ShouldRetry"/> is true (failed, under the attempt cap, and the backoff delay
        /// has elapsed) — guarding against premature or exhausted retries.
        /// </summary>
        public void PrepareForRetry()
        {
            if (!ShouldRetry())
                throw new InvalidOperationException(
                    $"Notification {Id} is not eligible for retry (Status={Status}, Attempts={AttemptCount}/{MaxRetryAttempts}).");

            Status = NotificationStatus.Queued;
        }

        /// <summary>
        /// Moves an exhausted, still-failing notification to the dead-letter queue (terminal). It is never retried
        /// automatically; it is surfaced for observability/support and can be replayed manually. Requires the retry
        /// budget to be exhausted so we never dead-letter a notification that could still succeed.
        /// </summary>
        public void MarkAsDeadLettered(string reason)
        {
            if (Status is NotificationStatus.Delivered or NotificationStatus.Read or NotificationStatus.Sent)
                throw new InvalidOperationException($"Cannot dead-letter a {Status} notification.");

            if (!HasExhaustedRetries())
                throw new InvalidOperationException(
                    $"Cannot dead-letter notification {Id}: retries not exhausted ({AttemptCount}/{MaxRetryAttempts}).");

            Status = NotificationStatus.DeadLettered;
            ErrorMessage = reason;

            RaiseDomainEvent(new NotificationFailedEvent(
                Id, RecipientId, Type, Channel, $"Dead-lettered: {reason}", AttemptCount));
        }

        public bool IsExpired()
        {
            return ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
        }

        public bool IsScheduledForNow()
        {
            if (!ScheduledFor.HasValue)
                return true;

            return DateTime.UtcNow >= ScheduledFor.Value;
        }
    }
}

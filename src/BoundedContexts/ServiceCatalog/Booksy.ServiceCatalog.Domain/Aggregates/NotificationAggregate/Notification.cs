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

        public bool ShouldRetry()
        {
            if (Status != NotificationStatus.Failed)
                return false;

            if (AttemptCount >= 5) // Max retry attempts
                return false;

            var lastAttempt = _deliveryAttempts.LastOrDefault();
            return lastAttempt?.ShouldRetry() ?? false;
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

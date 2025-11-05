// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/NotificationAggregate/Entities/DeliveryAttempt.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate.Entities
{
    /// <summary>
    /// Represents a delivery attempt for a notification with retry logic
    /// </summary>
    public sealed class DeliveryAttempt : Entity<Guid>
    {
        public int AttemptNumber { get; private set; }
        public DateTime AttemptedAt { get; private set; }
        public NotificationChannel Channel { get; private set; }
        public NotificationStatus Status { get; private set; }
        public string? GatewayMessageId { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? ErrorCode { get; private set; }
        public int? HttpStatusCode { get; private set; }
        public Dictionary<string, string> Metadata { get; private set; }
        public DateTime? NextRetryAt { get; private set; }
        public int RetryDelaySeconds { get; private set; }

        // For EF Core
        private DeliveryAttempt() : base(Guid.NewGuid())
        {
            Metadata = new Dictionary<string, string>();
        }

        private DeliveryAttempt(
            int attemptNumber,
            NotificationChannel channel) : base(Guid.NewGuid())
        {
            AttemptNumber = attemptNumber;
            Channel = channel;
            AttemptedAt = DateTime.UtcNow;
            Status = NotificationStatus.Queued;
            Metadata = new Dictionary<string, string>();
            RetryDelaySeconds = CalculateRetryDelay(attemptNumber);
        }

        public static DeliveryAttempt Create(int attemptNumber, NotificationChannel channel)
        {
            if (attemptNumber < 1)
                throw new ArgumentException("Attempt number must be at least 1", nameof(attemptNumber));

            return new DeliveryAttempt(attemptNumber, channel);
        }

        public void MarkAsSent(string gatewayMessageId)
        {
            Status = NotificationStatus.Sent;
            GatewayMessageId = gatewayMessageId;
        }

        public void MarkAsDelivered()
        {
            Status = NotificationStatus.Delivered;
        }

        public void MarkAsRead()
        {
            Status = NotificationStatus.Read;
        }

        public void MarkAsFailed(string errorMessage, string? errorCode = null, int? httpStatusCode = null)
        {
            Status = NotificationStatus.Failed;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
            HttpStatusCode = httpStatusCode;

            // Calculate next retry time using exponential backoff
            if (AttemptNumber < 5) // Max 5 attempts
            {
                NextRetryAt = DateTime.UtcNow.AddSeconds(RetryDelaySeconds);
            }
        }

        public void MarkAsBounced(string reason)
        {
            Status = NotificationStatus.Bounced;
            ErrorMessage = reason;
        }

        public void AddMetadata(string key, string value)
        {
            Metadata[key] = value;
        }

        /// <summary>
        /// Calculate retry delay using exponential backoff
        /// Attempt 1: 5 seconds
        /// Attempt 2: 15 seconds
        /// Attempt 3: 45 seconds
        /// Attempt 4: 135 seconds (2.25 minutes)
        /// Attempt 5: 405 seconds (6.75 minutes)
        /// </summary>
        private static int CalculateRetryDelay(int attemptNumber)
        {
            if (attemptNumber <= 1)
                return 5;

            // Exponential backoff: 5 * (3^(attemptNumber-1))
            return (int)(5 * Math.Pow(3, attemptNumber - 1));
        }

        public bool ShouldRetry()
        {
            return Status == NotificationStatus.Failed &&
                   AttemptNumber < 5 &&
                   NextRetryAt.HasValue &&
                   DateTime.UtcNow >= NextRetryAt.Value;
        }
    }
}

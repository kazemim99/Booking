namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications
{
    /// <summary>
    /// One row per (originating event, channel, recipient): the de-duplication gate and the durable delivery log.
    /// </summary>
    /// <remarks>
    /// The composite primary key is the atomic serialization point — exactly one concurrent insert for a tuple
    /// wins, so two dispatches of the same lifecycle event cannot both send. The row also survives the send as
    /// the audit trail support needs ("did the customer get the refund SMS, and what did the gateway say?").
    /// </remarks>
    public sealed class NotificationDelivery
    {
        /// <summary>The originating event id, or the notification's own id when it had no event.</summary>
        public Guid EventId { get; set; }

        /// <summary>A single channel name (never a combined flag value).</summary>
        public string Channel { get; set; } = default!;

        /// <summary>Email address, phone number, or user id — whatever the channel actually delivers to.</summary>
        public string Recipient { get; set; } = default!;

        public Guid NotificationId { get; set; }

        /// <summary>"Pending" while an attempt is in flight, "Delivered" once accepted, "Failed" after a failure.</summary>
        public string Status { get; set; } = Pending;

        public int AttemptCount { get; set; }
        public string? GatewayMessageId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public const string Pending = "Pending";
        public const string Delivered = "Delivered";
        public const string Failed = "Failed";
    }
}

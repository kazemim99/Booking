// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Notifications/SendNotification/SendNotificationCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Notifications.SendNotification
{
    /// <summary>
    /// Command to send a notification immediately
    /// </summary>
    public sealed record SendNotificationCommand(
        Guid RecipientId,
        NotificationType Type,
        NotificationChannel Channel,
        string Subject,
        string Body,
        NotificationPriority Priority = NotificationPriority.Normal,
        string? PlainTextBody = null,
        string? RecipientEmail = null,
        string? RecipientPhone = null,
        string? RecipientName = null,
        string? TemplateId = null,
        Dictionary<string, string>? TemplateData = null,
        Guid? BookingId = null,
        Guid? PaymentId = null,
        Guid? ProviderId = null,
        Dictionary<string, string>? Metadata = null,
        Guid? IdempotencyKey = null,

        /// <summary>
        /// Which catalogued notification this is. Set by the outbox sweep; null for direct API sends, which
        /// keep the older type-based suppression behaviour.
        /// </summary>
        NotificationEventCode? EventCode = null) : ICommand<SendNotificationResult>;
}

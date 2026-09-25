// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Notifications/SendBulkNotification/SendBulkNotificationCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Notifications.SendBulkNotification
{
    /// <summary>
    /// Command to send notification to multiple recipients
    /// </summary>
    public sealed record SendBulkNotificationCommand(
        List<Guid> RecipientIds,
        NotificationType Type,
        NotificationChannel Channel,
        string Subject,
        string Body,
        NotificationPriority Priority = NotificationPriority.Normal,
        string? PlainTextBody = null,
        string? TemplateId = null,
        Dictionary<string, string>? TemplateData = null,
        string? CampaignId = null,
        Dictionary<string, string>? Metadata = null,
        Guid? IdempotencyKey = null) : ICommand<SendBulkNotificationResult>;
}

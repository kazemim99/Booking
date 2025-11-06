// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/SendBulkNotification/SendBulkNotificationCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.SendBulkNotification
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

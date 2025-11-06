// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/SendNotification/SendNotificationCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.SendNotification
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
        Guid? IdempotencyKey = null) : ICommand<SendNotificationResult>;
}

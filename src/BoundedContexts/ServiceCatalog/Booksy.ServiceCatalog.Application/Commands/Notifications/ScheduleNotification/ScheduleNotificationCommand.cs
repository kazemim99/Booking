// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/ScheduleNotification/ScheduleNotificationCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.ScheduleNotification
{
    /// <summary>
    /// Command to schedule a notification for future delivery
    /// </summary>
    public sealed record ScheduleNotificationCommand(
        Guid RecipientId,
        NotificationType Type,
        NotificationChannel Channel,
        string Subject,
        string Body,
        DateTime ScheduledFor,
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
        Dictionary<string, string>? Metadata = null) : ICommand<ScheduleNotificationResult>;
}

// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/ResendNotification/ResendNotificationCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.ResendNotification
{
    /// <summary>
    /// Command to resend a failed notification
    /// </summary>
    public sealed record ResendNotificationCommand(
        Guid NotificationId,
        Guid? IdempotencyKey = null) : ICommand<ResendNotificationResult>;
}

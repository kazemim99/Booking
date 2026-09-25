// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Notifications/ResendNotification/ResendNotificationCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Notifications.ResendNotification
{
    /// <summary>
    /// Command to resend a failed notification
    /// </summary>
    public sealed record ResendNotificationCommand(
        Guid NotificationId,
        Guid? IdempotencyKey = null) : ICommand<ResendNotificationResult>;
}

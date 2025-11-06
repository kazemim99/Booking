// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/CancelNotification/CancelNotificationCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.CancelNotification
{
    /// <summary>
    /// Command to cancel a pending or queued notification
    /// </summary>
    public sealed record CancelNotificationCommand(
        Guid NotificationId,
        string? Reason = null,
        Guid? IdempotencyKey = null) : ICommand<CancelNotificationResult>;
}

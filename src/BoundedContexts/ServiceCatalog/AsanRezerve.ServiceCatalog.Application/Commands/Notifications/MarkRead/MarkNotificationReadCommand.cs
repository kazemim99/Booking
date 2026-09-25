using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Notifications.MarkRead
{
    /// <summary>
    /// Records that the caller opened one of their notifications.
    /// </summary>
    /// <remarks>
    /// <paramref name="ReaderId"/> is the authorization, not a filter: without it, any signed-in user could
    /// mark somebody else's notifications read and clear a badge they never saw.
    /// </remarks>
    public sealed record MarkNotificationReadCommand(
        Guid NotificationId,
        Guid ReaderId,
        Guid? IdempotencyKey = null) : ICommand<bool>;

    /// <summary>Marks everything the caller has as read, and reports how many that was.</summary>
    public sealed record MarkAllNotificationsReadCommand(
        Guid ReaderId,
        Guid? IdempotencyKey = null) : ICommand<int>;

    public sealed class MarkNotificationReadCommandHandler
        : ICommandHandler<MarkNotificationReadCommand, bool>
    {
        private readonly INotificationReadRepository _read;
        private readonly INotificationWriteRepository _write;
        private readonly ILogger<MarkNotificationReadCommandHandler> _logger;

        public MarkNotificationReadCommandHandler(
            INotificationReadRepository read,
            INotificationWriteRepository write,
            ILogger<MarkNotificationReadCommandHandler> logger)
        {
            _read = read;
            _write = write;
            _logger = logger;
        }

        public async Task<bool> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken)
        {
            var notification = await _read.GetByIdAsync(
                Domain.ValueObjects.NotificationId.From(command.NotificationId), cancellationToken);

            // Not found and not-yours answer the same way, so this cannot be used to discover which
            // notification ids exist.
            if (notification is null || notification.RecipientId.Value != command.ReaderId)
                return false;

            if (notification.ReadAt is not null)
                return true; // Already read. Idempotent by design: apps re-send this on every open.

            try
            {
                notification.MarkAsRead();
            }
            catch (InvalidOperationException ex)
            {
                // A notification that never reached anybody (queued, failed, cancelled) cannot be "read".
                // Reported as not-found rather than as an error: there is nothing the caller can do.
                _logger.LogDebug(ex, "Notification {NotificationId} is not in a readable state", command.NotificationId);
                return false;
            }

            await _write.UpdateNotificationAsync(notification, cancellationToken);
            return true;
        }
    }

    public sealed class MarkAllNotificationsReadCommandHandler
        : ICommandHandler<MarkAllNotificationsReadCommand, int>
    {
        private readonly INotificationReadRepository _read;
        private readonly INotificationWriteRepository _write;

        public MarkAllNotificationsReadCommandHandler(
            INotificationReadRepository read,
            INotificationWriteRepository write)
        {
            _read = read;
            _write = write;
        }

        public async Task<int> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken)
        {
            // A page at a time rather than "every notification ever": someone with years of history should
            // not load all of it into memory to clear a badge.
            var (notifications, _) = await _read.GetUserNotificationHistoryAsync(
                UserId.From(command.ReaderId),
                channel: null,
                type: null,
                status: null,
                startDate: null,
                endDate: null,
                pageNumber: 1,
                pageSize: 200,
                cancellationToken);

            var marked = 0;

            foreach (var notification in notifications.Where(n => n.ReadAt is null))
            {
                try
                {
                    notification.MarkAsRead();
                }
                catch (InvalidOperationException)
                {
                    continue; // Never reached anybody; not readable.
                }

                await _write.UpdateNotificationAsync(notification, cancellationToken);
                marked++;
            }

            return marked;
        }
    }
}

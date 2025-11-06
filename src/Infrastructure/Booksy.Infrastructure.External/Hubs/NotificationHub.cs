// ========================================
// Booksy.Infrastructure.External/Hubs/NotificationHub.cs
// ========================================
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.External.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time notifications
    /// </summary>
    public sealed class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("sub")?.Value
                        ?? Context.User?.FindFirst("userId")?.Value
                        ?? Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                // Add user to their personal group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                _logger.LogInformation(
                    "User {UserId} connected to NotificationHub with ConnectionId: {ConnectionId}",
                    userId,
                    Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning(
                    "Anonymous user connected to NotificationHub with ConnectionId: {ConnectionId}",
                    Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("sub")?.Value
                        ?? Context.User?.FindFirst("userId")?.Value
                        ?? Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

                _logger.LogInformation(
                    "User {UserId} disconnected from NotificationHub with ConnectionId: {ConnectionId}",
                    userId,
                    Context.ConnectionId);
            }

            if (exception != null)
            {
                _logger.LogError(exception,
                    "User disconnected with error. ConnectionId: {ConnectionId}",
                    Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        public async Task MarkAsRead(Guid notificationId)
        {
            var userId = Context.User?.FindFirst("sub")?.Value
                        ?? Context.User?.FindFirst("userId")?.Value;

            _logger.LogInformation(
                "User {UserId} marked notification {NotificationId} as read",
                userId,
                notificationId);

            // Notify all connections for this user
            if (!string.IsNullOrEmpty(userId))
            {
                await Clients.Group($"user_{userId}")
                    .SendAsync("NotificationRead", notificationId);
            }
        }

        /// <summary>
        /// Mark multiple notifications as read
        /// </summary>
        public async Task MarkMultipleAsRead(List<Guid> notificationIds)
        {
            var userId = Context.User?.FindFirst("sub")?.Value
                        ?? Context.User?.FindFirst("userId")?.Value;

            _logger.LogInformation(
                "User {UserId} marked {Count} notifications as read",
                userId,
                notificationIds.Count);

            // Notify all connections for this user
            if (!string.IsNullOrEmpty(userId))
            {
                await Clients.Group($"user_{userId}")
                    .SendAsync("MultipleNotificationsRead", notificationIds);
            }
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        public async Task MarkAllAsRead()
        {
            var userId = Context.User?.FindFirst("sub")?.Value
                        ?? Context.User?.FindFirst("userId")?.Value;

            _logger.LogInformation("User {UserId} marked all notifications as read", userId);

            // Notify all connections for this user
            if (!string.IsNullOrEmpty(userId))
            {
                await Clients.Group($"user_{userId}")
                    .SendAsync("AllNotificationsRead");
            }
        }
    }
}

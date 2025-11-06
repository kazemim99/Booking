// ========================================
// Booksy.ServiceCatalog.Infrastructure/Notifications/InAppNotificationService.cs
// ========================================
using Booksy.Infrastructure.External.Hubs;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Notifications
{
    /// <summary>
    /// Service for sending real-time in-app notifications via SignalR
    /// This implementation belongs to ServiceCatalog bounded context
    /// </summary>
    public sealed class InAppNotificationService : IInAppNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<InAppNotificationService> _logger;

        public InAppNotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<InAppNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<(bool Success, string? ErrorMessage)> SendToUserAsync(
            Guid userId,
            string title,
            string message,
            string type,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var notification = new
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Message = message,
                    Type = type,
                    Timestamp = DateTime.UtcNow,
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                await _hubContext.Clients
                    .Group($"user_{userId}")
                    .SendAsync("ReceiveNotification", notification, cancellationToken);

                _logger.LogInformation(
                    "Sent in-app notification to user {UserId}: {Title}",
                    userId,
                    title);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send in-app notification to user {UserId}",
                    userId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> SendToUsersAsync(
            List<Guid> userIds,
            string title,
            string message,
            string type,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var notification = new
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Message = message,
                    Type = type,
                    Timestamp = DateTime.UtcNow,
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                var tasks = userIds.Select(userId =>
                    _hubContext.Clients
                        .Group($"user_{userId}")
                        .SendAsync("ReceiveNotification", notification, cancellationToken));

                await Task.WhenAll(tasks);

                _logger.LogInformation(
                    "Sent in-app notification to {Count} users: {Title}",
                    userIds.Count,
                    title);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send in-app notification to multiple users");
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> SendToAllAsync(
            string title,
            string message,
            string type,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var notification = new
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Message = message,
                    Type = type,
                    Timestamp = DateTime.UtcNow,
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                await _hubContext.Clients.All
                    .SendAsync("ReceiveNotification", notification, cancellationToken);

                _logger.LogInformation("Broadcast in-app notification to all users: {Title}", title);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast in-app notification");
                return (false, ex.Message);
            }
        }

        public Task<bool> IsUserConnectedAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Note: SignalR doesn't provide a built-in way to check if a group has connected clients
            // This would require maintaining a separate connection tracking mechanism
            // For now, return true and rely on SignalR's internal handling
            return Task.FromResult(true);
        }
    }
}

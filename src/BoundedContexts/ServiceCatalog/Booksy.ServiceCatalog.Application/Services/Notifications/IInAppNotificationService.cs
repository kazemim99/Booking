// ========================================
// Booksy.ServiceCatalog.Application/Services/Notifications/IInAppNotificationService.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// Service for sending real-time in-app notifications via SignalR
    /// </summary>
    public interface IInAppNotificationService
    {
        /// <summary>
        /// Send in-app notification to a specific user
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> SendToUserAsync(
            Guid userId,
            string title,
            string message,
            string type,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Send in-app notification to multiple users
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> SendToUsersAsync(
            List<Guid> userIds,
            string title,
            string message,
            string type,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Send in-app notification to all connected users (broadcast)
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> SendToAllAsync(
            string title,
            string message,
            string type,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if user is currently connected to SignalR
        /// </summary>
        Task<bool> IsUserConnectedAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}

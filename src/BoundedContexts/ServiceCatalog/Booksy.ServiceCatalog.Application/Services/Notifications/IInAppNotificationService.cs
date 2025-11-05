// ========================================
// Booksy.ServiceCatalog.Application/Services/Notifications/IInAppNotificationService.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// Service for sending in-app notifications via SignalR
    /// </summary>
    public interface IInAppNotificationService
    {
        /// <summary>
        /// Send in-app notification to a specific user
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> SendToUserAsync(
            Guid userId,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Send in-app notification to multiple users
        /// </summary>
        Task<List<(Guid UserId, bool Success, string? ErrorMessage)>> SendToUsersAsync(
            List<Guid> userIds,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Send in-app notification to all users in a group
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> SendToGroupAsync(
            string groupName,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            CancellationToken cancellationToken = default);
    }
}

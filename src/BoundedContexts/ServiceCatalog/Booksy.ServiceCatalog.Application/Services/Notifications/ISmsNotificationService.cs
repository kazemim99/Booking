// ========================================
// Booksy.ServiceCatalog.Application/Services/Notifications/ISmsNotificationService.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// Service for sending SMS notifications
    /// </summary>
    public interface ISmsNotificationService
    {
        /// <summary>
        /// Send an SMS notification
        /// </summary>
        Task<(bool Success, string? MessageId, string? ErrorMessage)> SendSmsAsync(
            string phoneNumber,
            string message,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Send bulk SMS messages
        /// </summary>
        Task<List<(string PhoneNumber, bool Success, string? MessageId, string? ErrorMessage)>> SendBulkSmsAsync(
            List<string> phoneNumbers,
            string message,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default);
    }
}

// ========================================
// Booksy.UserManagement.Application/Services/Interfaces/ISmsNotificationService.cs
// ========================================
namespace Booksy.UserManagement.Application.Services.Interfaces
{
    /// <summary>
    /// Service for sending SMS notifications within the UserManagement bounded context
    /// </summary>
    public interface ISmsNotificationService
    {
        /// <summary>
        /// Send an SMS notification
        /// </summary>
        /// <param name="phoneNumber">Recipient phone number</param>
        /// <param name="message">SMS message content</param>
        /// <param name="metadata">Optional metadata for tracking and logging</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tuple with success status, message ID, and error message if any</returns>
        Task<(bool Success, string? MessageId, string? ErrorMessage)> SendSmsAsync(
            string phoneNumber,
            string message,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Send bulk SMS messages
        /// </summary>
        /// <param name="phoneNumbers">List of recipient phone numbers</param>
        /// <param name="message">SMS message content</param>
        /// <param name="metadata">Optional metadata for tracking and logging</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of results for each phone number</returns>
        Task<List<(string PhoneNumber, bool Success, string? MessageId, string? ErrorMessage)>> SendBulkSmsAsync(
            List<string> phoneNumbers,
            string message,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default);
    }
}

// ========================================
// Booksy.Core.Application/Services/Notifications/ISmsNotificationService.cs
// ========================================
namespace Booksy.Core.Application.Services.Notifications
{
    /// <summary>
    /// The single SMS-sending abstraction for the whole monolith.
    /// </summary>
    /// <remarks>
    /// This port used to exist three times — once in <c>ServiceCatalog.Application.Services</c> (with dead
    /// booking-template methods), once in <c>ServiceCatalog.Application.Services.Notifications</c>, and once in
    /// <c>UserManagement.Application.Services.Interfaces</c> — each with its own implementation and DI registration.
    /// In a single-process monolith that meant two live SMS gateways and DI resolution depending on which
    /// <c>using</c> a caller happened to pick. It lives in the shared Core.Application layer so every bounded
    /// context binds to the same contract and exactly one implementation is registered.
    /// </remarks>
    public interface ISmsNotificationService
    {
        /// <summary>
        /// Send an SMS notification. Never throws for gateway failures: the outcome is reported in the tuple so
        /// callers can record the attempt and decide whether to retry.
        /// </summary>
        /// <param name="phoneNumber">Recipient phone number</param>
        /// <param name="message">SMS message content</param>
        /// <param name="metadata">Optional metadata for tracking and logging</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<(bool Success, string? MessageId, string? ErrorMessage)> SendSmsAsync(
            string phoneNumber,
            string message,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Send the same message to many recipients, reporting a per-recipient outcome.
        /// </summary>
        Task<List<(string PhoneNumber, bool Success, string? MessageId, string? ErrorMessage)>> SendBulkSmsAsync(
            List<string> phoneNumbers,
            string message,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default);
    }
}

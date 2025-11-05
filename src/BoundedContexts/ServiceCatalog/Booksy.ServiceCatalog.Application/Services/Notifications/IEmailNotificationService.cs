// ========================================
// Booksy.ServiceCatalog.Application/Services/Notifications/IEmailNotificationService.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// Service for sending email notifications
    /// </summary>
    public interface IEmailNotificationService
    {
        /// <summary>
        /// Send an email notification
        /// </summary>
        Task<(bool Success, string? MessageId, string? ErrorMessage)> SendEmailAsync(
            string to,
            string subject,
            string htmlBody,
            string? plainTextBody = null,
            string? fromName = null,
            Dictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Send bulk emails
        /// </summary>
        Task<List<(string Email, bool Success, string? MessageId, string? ErrorMessage)>> SendBulkEmailAsync(
            List<string> recipients,
            string subject,
            string htmlBody,
            string? plainTextBody = null,
            Dictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default);
    }
}

// ========================================
// Booksy.ServiceCatalog.Infrastructure/Notifications/Email/SendGridEmailNotificationService.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Booksy.ServiceCatalog.Infrastructure.Notifications.Email
{
    /// <summary>
    /// SendGrid implementation of email notification service
    /// </summary>
    public sealed class SendGridEmailNotificationService : IEmailNotificationService
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly ILogger<SendGridEmailNotificationService> _logger;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SendGridEmailNotificationService(
            ISendGridClient sendGridClient,
            IConfiguration configuration,
            ILogger<SendGridEmailNotificationService> logger)
        {
            _sendGridClient = sendGridClient;
            _logger = logger;
            _fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@booksy.com";
            _fromName = configuration["SendGrid:FromName"] ?? "Booksy";
        }

        public async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendEmailAsync(
            string to,
            string subject,
            string htmlBody,
            string? plainTextBody = null,
            string? fromName = null,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var from = new EmailAddress(_fromEmail, fromName ?? _fromName);
                var toAddress = new EmailAddress(to);

                var msg = MailHelper.CreateSingleEmail(
                    from,
                    toAddress,
                    subject,
                    plainTextBody ?? StripHtmlTags(htmlBody),
                    htmlBody);

                // Add custom args for tracking
                if (metadata != null)
                {
                    foreach (var kvp in metadata)
                    {
                        msg.AddCustomArg(kvp.Key, kvp.Value?.ToString() ?? "");
                    }
                }

                var response = await _sendGridClient.SendEmailAsync(msg, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var messageId = response.Headers.GetValues("X-Message-Id").FirstOrDefault();
                    _logger.LogInformation("Email sent successfully to {To}. MessageId: {MessageId}", to, messageId);
                    return (true, messageId, null);
                }
                else
                {
                    var errorBody = await response.Body.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Failed to send email to {To}. Status: {Status}, Error: {Error}",
                        to, response.StatusCode, errorBody);
                    return (false, null, $"SendGrid error: {response.StatusCode} - {errorBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while sending email to {To}", to);
                return (false, null, ex.Message);
            }
        }

        public async Task<List<(string Email, bool Success, string? MessageId, string? ErrorMessage)>> SendBulkEmailAsync(
            List<string> recipients,
            string subject,
            string htmlBody,
            string? plainTextBody = null,
            Dictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<(string Email, bool Success, string? MessageId, string? ErrorMessage)>();

            // SendGrid supports bulk sending, but for simplicity, we'll send individually
            // In production, use SendGrid's bulk API for better performance
            var objectMetadata = metadata?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
            foreach (var recipient in recipients)
            {
                var result = await SendEmailAsync(recipient, subject, htmlBody, plainTextBody, null, objectMetadata, cancellationToken);
                results.Add((recipient, result.Success, result.MessageId, result.ErrorMessage));
            }

            return results;
        }

        private static string StripHtmlTags(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // Simple HTML tag removal (for production, use HtmlAgilityPack)
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        }

      
    }
}

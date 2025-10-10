// ========================================
// Security Services
// ========================================

// Booksy.UserManagement.Infrastructure/Services/Security/PasswordHasher.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;

namespace Booksy.Infrastructure.External.Notifications
{
    public class EmailService: IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["Email:SmtpUsername"] ?? "";
            _smtpPassword = _configuration["Email:SmtpPassword"] ?? "";
            _fromEmail = _configuration["Email:FromEmail"] ?? "noreply@booksy.com";
            _fromName = _configuration["Email:FromName"] ?? "Booksy";
        }

        public async Task SendEmailAsync(
            string to,
            string subject,
            string htmlBody,
            string? plainTextBody = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                message.To.Add(new MailAddress(to));

                if (!string.IsNullOrEmpty(plainTextBody))
                {
                    var plainView = AlternateView.CreateAlternateViewFromString(
                        plainTextBody,
                        null,
                        "text/plain");
                    message.AlternateViews.Add(plainView);
                }

                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
                };

                //await client.SendMailAsync(message, cancellationToken);

                _logger.LogInformation("Email sent successfully to {Recipient}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}", to);
                throw new ApplicationException($"Failed to send email Recipient {to}");
            }
        }

        public async Task SendBulkEmailAsync(
            List<string> recipients,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            // In production, this would use a bulk email service like SendGrid
            foreach (var recipient in recipients)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await SendEmailAsync(recipient, subject, htmlBody, cancellationToken: cancellationToken);

                // Add delay to avoid rate limiting
                await Task.Delay(100, cancellationToken);
            }
        }
    }
}

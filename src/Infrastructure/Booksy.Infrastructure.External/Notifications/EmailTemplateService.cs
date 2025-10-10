// ========================================
// Security Services
// ========================================

// Booksy.UserManagement.Infrastructure/Services/Security/PasswordHasher.cs
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.External.Notifications
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailTemplateService> _logger;
        private readonly Dictionary<string, EmailTemplate> _templates;

        public EmailTemplateService(IEmailService emailService, ILogger<EmailTemplateService> logger)
        {
            _emailService = emailService;
            _logger = logger;
            _templates = InitializeTemplates();
        }

        public async Task SendEmailAsync(
            string to,
            string templateId,
            Dictionary<string, string> data,
            CancellationToken cancellationToken = default)
        {
            if (!_templates.TryGetValue(templateId, out var template))
            {
                throw new InvalidOperationException($"Email template '{templateId}' not found");
            }

            var (subject, htmlBody, plainTextBody) = template.Render(data);

            await _emailService.SendEmailAsync(to, subject, htmlBody, plainTextBody, cancellationToken);
        }

        public async Task SendBulkEmailAsync(
            List<string> recipients,
            string templateId,
            Dictionary<string, string> data,
            CancellationToken cancellationToken = default)
        {
            if (!_templates.TryGetValue(templateId, out var template))
            {
                throw new InvalidOperationException($"Email template '{templateId}' not found");
            }

            var (subject, htmlBody, _) = template.Render(data);

            await _emailService.SendBulkEmailAsync(recipients, subject, htmlBody, cancellationToken);
        }

        public Task<bool> IsEmailTemplateAvailableAsync(string templateId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_templates.ContainsKey(templateId));
        }

        private Dictionary<string, EmailTemplate> InitializeTemplates()
        {
            return new Dictionary<string, EmailTemplate>
            {
                [EmailTemplate.Templates.Welcome] = new EmailTemplate
                {
                    Id = EmailTemplate.Templates.Welcome,
                    Name = "Welcome Email",
                    Subject = "Welcome to Booksy, {{FirstName}}!",
                    HtmlBody = @"
                        <h1>Welcome to Booksy!</h1>
                        <p>Hi {{FirstName}},</p>
                        <p>Thank you for joining Booksy. We're excited to have you on board!</p>
                        <p>To get started, please activate your account:</p>
                        <p><a href='{{ActivationUrl}}'>Activate Account</a></p>
                        <p>Best regards,<br>The Booksy Team</p>",
                    PlainTextBody = @"
                        Welcome to Booksy!
                        Hi {{FirstName}},
                        Thank you for joining Booksy. To activate your account, visit:
                        {{ActivationUrl}}
                        Best regards,
                        The Booksy Team"
                },

                [EmailTemplate.Templates.EmailVerification] = new EmailTemplate
                {
                    Id = EmailTemplate.Templates.EmailVerification,
                    Name = "Email Verification",
                    Subject = "Verify your email address",
                    HtmlBody = @"
                        <h2>Email Verification</h2>
                        <p>Please verify your email address by clicking the link below:</p>
                        <p><a href='{{VerificationUrl}}'>Verify Email</a></p>
                        <p>Or enter this code: {{VerificationCode}}</p>",
                    PlainTextBody = @"
                        Email Verification
                        Verify your email at: {{VerificationUrl}}
                        Or use code: {{VerificationCode}}"
                },

                [EmailTemplate.Templates.PasswordReset] = new EmailTemplate
                {
                    Id = EmailTemplate.Templates.PasswordReset,
                    Name = "Password Reset",
                    Subject = "Reset your Booksy password",
                    HtmlBody = @"
                        <h2>Password Reset Request</h2>
                        <p>Hi {{FirstName}},</p>
                        <p>You requested to reset your password. Click the link below:</p>
                        <p><a href='{{ResetUrl}}'>Reset Password</a></p>
                        <p>This link will expire at {{ExpiresAt}}.</p>
                        <p>If you didn't request this, please ignore this email.</p>",
                    PlainTextBody = @"
                        Password Reset Request
                        Hi {{FirstName}},
                        Reset your password at: {{ResetUrl}}
                        This link expires at {{ExpiresAt}}.
                        If you didn't request this, please ignore this email."
                }
            };
        }
    }
}


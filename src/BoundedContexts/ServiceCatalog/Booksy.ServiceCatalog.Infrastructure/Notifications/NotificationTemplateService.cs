// ========================================
// Booksy.ServiceCatalog.Infrastructure/Notifications/NotificationTemplateService.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationTemplateAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Notifications
{
    /// <summary>
    /// Service for managing and rendering notification templates from database
    /// </summary>
    public class NotificationTemplateService : INotificationTemplateService
    {
        private readonly INotificationTemplateRepository _templateRepository;
        private readonly ITemplateEngine _templateEngine;
        private readonly ILogger<NotificationTemplateService> _logger;

        public NotificationTemplateService(
            INotificationTemplateRepository templateRepository,
            ITemplateEngine templateEngine,
            ILogger<NotificationTemplateService> logger)
        {
            _templateRepository = templateRepository;
            _templateEngine = templateEngine;
            _logger = logger;
        }

        public async Task<NotificationTemplate?> GetTemplateByKeyAsync(
            string templateKey,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(templateKey))
                throw new ArgumentException("Template key cannot be empty", nameof(templateKey));

            return await _templateRepository.GetByKeyAsync(templateKey, cancellationToken);
        }

        public async Task<NotificationTemplate?> GetLocalizedTemplateAsync(
            string templateKey,
            string languageCode,
            CancellationToken cancellationToken = default)
        {
            var template = await GetTemplateByKeyAsync(templateKey, cancellationToken);

            if (template == null)
                return null;

            // Check if localized version exists
            if (!string.IsNullOrEmpty(languageCode) &&
                template.LocalizedVersions.TryGetValue(languageCode, out var localizedTemplateId))
            {
                var localizedTemplate = await _templateRepository.GetByKeyAsync(localizedTemplateId, cancellationToken);
                if (localizedTemplate != null && localizedTemplate.IsActive)
                {
                    return localizedTemplate;
                }
            }

            // Fall back to default language template
            return template;
        }

        public async Task<string> RenderTemplateAsync(
            string templateKey,
            NotificationChannel channel,
            Dictionary<string, object> variables,
            string? languageCode = null,
            CancellationToken cancellationToken = default)
        {
            var template = await GetLocalizedTemplateAsync(templateKey, languageCode ?? "en", cancellationToken);

            if (template == null)
            {
                _logger.LogWarning("Template not found: {TemplateKey}", templateKey);
                throw new InvalidOperationException($"Template '{templateKey}' not found");
            }

            if (!template.SupportsChannel(channel))
            {
                _logger.LogWarning(
                    "Template {TemplateKey} does not support channel {Channel}",
                    templateKey,
                    channel);
                throw new InvalidOperationException($"Template '{templateKey}' does not support {channel} channel");
            }

            var templateContent = template.GetTemplateForChannel(channel);

            if (string.IsNullOrEmpty(templateContent))
            {
                _logger.LogWarning(
                    "Template {TemplateKey} has no content for channel {Channel}",
                    templateKey,
                    channel);
                throw new InvalidOperationException($"Template '{templateKey}' has no content for {channel} channel");
            }

            // Validate required variables
            var missingVariables = _templateEngine.ValidateVariables(template.RequiredVariables, variables);
            if (missingVariables.Any())
            {
                _logger.LogWarning(
                    "Missing required variables for template {TemplateKey}: {MissingVariables}",
                    templateKey,
                    string.Join(", ", missingVariables));
                throw new InvalidOperationException(
                    $"Missing required variables: {string.Join(", ", missingVariables)}");
            }

            // Render template
            var renderedContent = _templateEngine.Render(templateContent, variables);

            // Record usage
            template.RecordUsage();
            await _templateRepository.UpdateTemplateAsync(template, cancellationToken);

            return renderedContent;
        }

        public async Task<(string Subject, string BodyHtml, string? BodyPlainText)> RenderEmailTemplateAsync(
            string templateKey,
            Dictionary<string, object> variables,
            string? languageCode = null,
            CancellationToken cancellationToken = default)
        {
            var template = await GetLocalizedTemplateAsync(templateKey, languageCode ?? "en", cancellationToken);

            if (template == null)
            {
                _logger.LogWarning("Email template not found: {TemplateKey}", templateKey);
                throw new InvalidOperationException($"Template '{templateKey}' not found");
            }

            if (!template.SupportsChannel(NotificationChannel.Email))
            {
                _logger.LogWarning(
                    "Template {TemplateKey} does not support Email channel",
                    templateKey);
                throw new InvalidOperationException($"Template '{templateKey}' does not support Email channel");
            }

            if (string.IsNullOrEmpty(template.EmailBodyTemplate))
            {
                throw new InvalidOperationException($"Template '{templateKey}' has no email body");
            }

            // Validate required variables
            var missingVariables = _templateEngine.ValidateVariables(template.RequiredVariables, variables);
            if (missingVariables.Any())
            {
                _logger.LogWarning(
                    "Missing required variables for email template {TemplateKey}: {MissingVariables}",
                    templateKey,
                    string.Join(", ", missingVariables));
                throw new InvalidOperationException(
                    $"Missing required variables: {string.Join(", ", missingVariables)}");
            }

            // Render all email components
            var subject = !string.IsNullOrEmpty(template.EmailSubjectTemplate)
                ? _templateEngine.Render(template.EmailSubjectTemplate, variables)
                : "Notification";

            var bodyHtml = _templateEngine.Render(template.EmailBodyTemplate, variables);

            var bodyPlainText = !string.IsNullOrEmpty(template.EmailPlainTextTemplate)
                ? _templateEngine.Render(template.EmailPlainTextTemplate, variables)
                : null;

            // Record usage
            template.RecordUsage();
            await _templateRepository.UpdateTemplateAsync(template, cancellationToken);

            _logger.LogInformation(
                "Rendered email template {TemplateKey} for {RecipientCount} variables",
                templateKey,
                variables.Count);

            return (subject, bodyHtml, bodyPlainText);
        }
    }
}

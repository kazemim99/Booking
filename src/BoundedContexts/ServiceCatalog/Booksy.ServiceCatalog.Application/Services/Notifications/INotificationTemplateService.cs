// ========================================
// Booksy.ServiceCatalog.Application/Services/Notifications/INotificationTemplateService.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationTemplateAggregate;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// Service for managing and retrieving notification templates
    /// </summary>
    public interface INotificationTemplateService
    {
        /// <summary>
        /// Gets an active template by its unique key
        /// </summary>
        Task<NotificationTemplate?> GetTemplateByKeyAsync(string templateKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a localized version of a template
        /// </summary>
        Task<NotificationTemplate?> GetLocalizedTemplateAsync(string templateKey, string languageCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Renders a template for a specific channel with provided data
        /// </summary>
        Task<string> RenderTemplateAsync(
            string templateKey,
            NotificationChannel channel,
            Dictionary<string, object> variables,
            string? languageCode = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Renders email subject and body
        /// </summary>
        Task<(string Subject, string BodyHtml, string? BodyPlainText)> RenderEmailTemplateAsync(
            string templateKey,
            Dictionary<string, object> variables,
            string? languageCode = null,
            CancellationToken cancellationToken = default);
    }
}

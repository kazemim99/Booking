// ========================================
// AsanRezerve.ServiceCatalog.Domain/Repositories/INotificationTemplateRepository.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.NotificationTemplateAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Repositories
{
    /// <summary>
    /// Repository for NotificationTemplate aggregate
    /// </summary>
    public interface INotificationTemplateRepository : IWriteRepository<NotificationTemplate, TemplateId>
    {
        /// <summary>
        /// Get template by ID
        /// </summary>
        Task<NotificationTemplate?> GetByIdAsync(TemplateId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get template by unique key
        /// </summary>
        Task<NotificationTemplate?> GetByKeyAsync(string templateKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get active template for a notification type and channel
        /// </summary>
        Task<NotificationTemplate?> GetActiveTemplateAsync(NotificationType type, NotificationChannel channel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all active templates
        /// </summary>
        Task<List<NotificationTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Save template
        /// </summary>
        Task SaveTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update template
        /// </summary>
        Task UpdateTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);
    }
}

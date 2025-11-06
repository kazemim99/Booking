// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/NotificationTemplateRepository.cs
// ========================================
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationTemplateAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository for NotificationTemplate aggregate
    /// </summary>
    public sealed class NotificationTemplateRepository
        : EfWriteRepositoryBase<NotificationTemplate, TemplateId, ServiceCatalogDbContext>,
          INotificationTemplateRepository
    {
        private readonly ILogger<NotificationTemplateRepository> _logger;

        public NotificationTemplateRepository(
            ServiceCatalogDbContext context,
            ILogger<NotificationTemplateRepository> logger)
            : base(context)
        {
            _logger = logger;
        }

        public async Task<NotificationTemplate?> GetByIdAsync(
            TemplateId id,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task<NotificationTemplate?> GetByKeyAsync(
            string templateKey,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(t => t.TemplateKey == templateKey && t.IsActive)
                .OrderByDescending(t => t.TemplateVersion)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<NotificationTemplate?> GetActiveTemplateAsync(
            NotificationType type,
            NotificationChannel channel,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(t => t.Type == type &&
                           t.IsActive &&
                           (t.SupportedChannels & channel) == channel)
                .OrderByDescending(t => t.TemplateVersion)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<NotificationTemplate>> GetActiveTemplatesAsync(
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task SaveTemplateAsync(
            NotificationTemplate template,
            CancellationToken cancellationToken = default)
        {
            await SaveAsync(template, cancellationToken);
            _logger.LogInformation(
                "Saved notification template: {TemplateKey} (v{Version})",
                template.TemplateKey,
                template.TemplateVersion);
        }

        public async Task UpdateTemplateAsync(
            NotificationTemplate template,
            CancellationToken cancellationToken = default)
        {
            await UpdateAsync(template, cancellationToken);
            _logger.LogDebug(
                "Updated notification template: {TemplateKey} (v{Version})",
                template.TemplateKey,
                template.TemplateVersion);
        }
    }
}

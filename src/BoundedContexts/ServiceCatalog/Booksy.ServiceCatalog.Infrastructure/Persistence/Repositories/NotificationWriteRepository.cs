// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/NotificationWriteRepository.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Implementation of Notification write repository
    /// </summary>
    public sealed class NotificationWriteRepository : INotificationWriteRepository
    {
        private readonly ServiceCatalogDbContext _context;

        public NotificationWriteRepository(ServiceCatalogDbContext context)
        {
            _context = context;
        }

        public async Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Notification>()
                .Include(n => n.DeliveryAttempts)
                .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        }

        public async Task SaveNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            await _context.Set<Notification>().AddAsync(notification, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            _context.Set<Notification>().Update(notification);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<Notification>> GetPendingNotificationsAsync(int batchSize = 100, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Notification>()
                .Where(n => n.Status == NotificationStatus.Pending || n.Status == NotificationStatus.Queued)
                .OrderBy(n => n.Priority)
                .ThenBy(n => n.CreatedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Notification>> GetScheduledNotificationsToSendAsync(DateTime currentTime, int batchSize = 100, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Notification>()
                .Where(n =>
                    n.Status == NotificationStatus.Queued &&
                    n.ScheduledFor.HasValue &&
                    n.ScheduledFor.Value <= currentTime)
                .OrderBy(n => n.ScheduledFor)
                .ThenBy(n => n.Priority)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Notification>> GetExpiredNotificationsAsync(DateTime currentTime, int batchSize = 100, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Notification>()
                .Where(n =>
                    n.Status != NotificationStatus.Delivered &&
                    n.Status != NotificationStatus.Cancelled &&
                    n.Status != NotificationStatus.Expired &&
                    n.ExpiresAt.HasValue &&
                    n.ExpiresAt.Value <= currentTime)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        public Task AddAsync(Notification entity, CancellationToken cancellationToken = default)
        {
            return SaveNotificationAsync(entity, cancellationToken);
        }

        public Task UpdateAsync(Notification entity, CancellationToken cancellationToken = default)
        {
            return UpdateNotificationAsync(entity, cancellationToken);
        }

        public async Task DeleteAsync(Notification entity, CancellationToken cancellationToken = default)
        {
            _context.Set<Notification>().Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

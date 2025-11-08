using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class ProviderWriteRepository : EfWriteRepositoryBase<Provider, ProviderId, ServiceCatalogDbContext>, IProviderWriteRepository
    {
        public ProviderWriteRepository(
            ServiceCatalogDbContext context,
            ILogger<ProviderWriteRepository> logger)
            : base(context)
        {
        }

        public new async Task<Provider?> GetByIdAsync(ProviderId id, CancellationToken cancellationToken = default)
        {
            // Include all navigation properties and owned collections
            return await DbSet
                .Include(p => p.Staff)
                .Include(p => p.BusinessHours)
                .Include(p => p.Holidays)
                .Include(p => p.Exceptions)
                .Include(p => p.Profile)
                                    .ThenInclude(profile => profile.GalleryImages)

                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public new async Task<Provider?> GetByOwnerIdAsync(UserId id, CancellationToken cancellationToken = default)
        {
            // Include all navigation properties and owned collections
            return await DbSet
                .Include(p => p.Staff)
                .Include(p => p.BusinessHours)
                .Include(p => p.Holidays)
                .Include(p => p.Exceptions)
                .Include(p => p.Profile)
                                                    .ThenInclude(profile => profile.GalleryImages)

                .FirstOrDefaultAsync(p => p.OwnerId == id, cancellationToken);
        }

        public async Task<Provider?> GetDraftProviderByOwnerIdAsync(UserId ownerId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.Staff)
                .Include(p => p.Services)
                .Include(p => p.BusinessHours)
                .Include(p => p.Profile)
                    .ThenInclude(profile => profile.GalleryImages)
                .FirstOrDefaultAsync(
                    p => p.OwnerId == ownerId && p.Status == Domain.Enums.ProviderStatus.Drafted,
                    cancellationToken);
        }

        public async Task SaveProviderAsync(Provider provider, CancellationToken cancellationToken = default)
        {
            await SaveAsync(provider, cancellationToken);
        }

        public async Task UpdateProviderAsync(Provider provider, CancellationToken cancellationToken = default)
        {
            // For owned entities with Field access mode, changes to owned collections
            // need explicit tracking. Mark the provider and owned entities as modified.

            var entry = Context.Entry(provider);

            // If detached, attach and mark as modified
            if (entry.State == EntityState.Detached)
            {
                Context.Attach(provider);
                entry.State = EntityState.Modified;
            }

            // Explicitly mark all gallery images as modified to ensure changes are persisted
            // This is necessary because GalleryImages uses PropertyAccessMode.Field
            foreach (var galleryImage in provider.Profile.GalleryImages)
            {
                var imageEntry = Context.Entry(galleryImage);
                if (imageEntry.State != EntityState.Added && imageEntry.State != EntityState.Deleted)
                {
                    imageEntry.State = EntityState.Modified;
                }
            }
        }

        public async Task DeleteProviderAsync(Provider provider, CancellationToken cancellationToken = default)
        {
            await RemoveAsync(provider, cancellationToken);
        }
    }
}

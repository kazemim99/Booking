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
            // For owned entities using PropertyAccessMode.Field, EF Core doesn't automatically
            // detect changes to properties on owned entities when they're accessed via the backing field.

            var entry = Context.Entry(provider);

            // If the entity is detached, attach it and mark as modified
            if (entry.State == EntityState.Detached)
            {
                Context.Update(provider);
            }
            else
            {
                // Entity is already tracked - just ensure changes are detected
                Context.ChangeTracker.DetectChanges();

                // Explicitly mark the provider as modified
                entry.State = EntityState.Modified;
            }

            // Mark the Profile owned entity as modified
            var profileEntry = entry.Reference(p => p.Profile).TargetEntry;
            if (profileEntry != null && profileEntry.State != EntityState.Detached)
            {
                profileEntry.State = EntityState.Modified;
            }

            // Explicitly mark ALL gallery images as modified to ensure
            // changes to their properties (like IsActive) are persisted
            // This is critical because GalleryImages uses PropertyAccessMode.Field
            foreach (var galleryImage in provider.Profile.GalleryImages)
            {
                var imageEntry = Context.Entry(galleryImage);
                if (imageEntry.State == EntityState.Detached)
                {
                    // Skip detached entities
                    continue;
                }

                if (imageEntry.State != EntityState.Added && imageEntry.State != EntityState.Deleted)
                {
                    // Mark the entire entity as modified
                    imageEntry.State = EntityState.Modified;

                    // Explicitly mark critical properties as modified to ensure they're updated
                    imageEntry.Property(gi => gi.IsActive).IsModified = true;
                    imageEntry.Property(gi => gi.DisplayOrder).IsModified = true;
                    imageEntry.Property(gi => gi.IsPrimary).IsModified = true;
                    imageEntry.Property(gi => gi.Caption).IsModified = true;
                    imageEntry.Property(gi => gi.AltText).IsModified = true;
                }
            }
        }

        public async Task DeleteProviderAsync(Provider provider, CancellationToken cancellationToken = default)
        {
            await RemoveAsync(provider, cancellationToken);
        }
    }
}

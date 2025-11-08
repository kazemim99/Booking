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
            // Since GalleryImages collection is now exposed directly (not through AsReadOnly),
            // EF Core can track changes. However, when entities are already tracked (loaded via
            // GetByIdAsync), we need to explicitly detect changes to owned entity properties.

            var entry = Context.Entry(provider);

            if (entry.State == EntityState.Detached)
            {
                // Entity not tracked yet, mark entire aggregate as updated
                Context.Update(provider);
            }
            else
            {
                // Entity already tracked - explicitly detect changes
                Context.ChangeTracker.DetectChanges();

                // Explicitly mark the Profile owned entity as modified
                var profileEntry = entry.Reference(p => p.Profile).TargetEntry;
                if (profileEntry != null)
                {
                    profileEntry.State = EntityState.Modified;

                    // Explicitly mark all GalleryImages owned entities as modified
                    // This ensures property changes (like IsActive = false) are persisted
                    var galleryImagesCollection = profileEntry.Collection(p => p.GalleryImages);
                    foreach (var imageEntry in galleryImagesCollection.CurrentValue!)
                    {
                        var imgEntry = Context.Entry(imageEntry);
                        if (imgEntry.State != EntityState.Added && imgEntry.State != EntityState.Deleted)
                        {
                            imgEntry.State = EntityState.Modified;
                        }
                    }
                }
            }
        }

        public async Task DeleteProviderAsync(Provider provider, CancellationToken cancellationToken = default)
        {
            await RemoveAsync(provider, cancellationToken);
        }
    }
}

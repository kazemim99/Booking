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
                .Include(p => p.Services)
                .Include(p => p.BusinessHours)
                .Include(p => p.Profile)
                    .ThenInclude(profile => profile.GalleryImages)
                .FirstOrDefaultAsync(
                    p => p.OwnerId == ownerId ,
                    cancellationToken);
        }

        public async Task SaveProviderAsync(Provider provider, CancellationToken cancellationToken = default)
        {
            await SaveAsync(provider, cancellationToken);
        }

        /// <summary>
        /// Updates a provider and handles owned entity change detection.
        ///
        /// IMPORTANT: This method includes a workaround for EF Core's limitation with owned entities.
        /// When items are removed from owned collections (like GalleryImages), EF Core doesn't
        /// automatically detect the deletion. We must:
        /// 1. Force change detection with DetectChanges()
        /// 2. Manually find removed entities in the ChangeTracker
        /// 3. Explicitly mark them as Deleted
        ///
        /// This is a known limitation of EF Core when using owned entities with backing fields.
        /// Reference: https://github.com/dotnet/efcore/issues/19856
        ///
        /// Alternative approaches considered:
        /// - Making GalleryImages a regular entity: Would break DDD aggregate boundaries
        /// - Using soft delete: Doesn't meet requirement for hard delete with physical file removal
        /// </summary>
        public Task UpdateProviderAsync(Provider provider, CancellationToken cancellationToken = default)
        {
            // Mark the provider as modified and force change detection
            Context.Update(provider);
            Context.ChangeTracker.DetectChanges();

            // WORKAROUND: Manually mark removed owned entities as deleted
            // Find all gallery images that are tracked but not in the current collection
            var currentGalleryIds = provider.Profile.GalleryImages.Select(g => g.Id).ToHashSet();

            var trackedGalleryImages = Context.ChangeTracker.Entries()
                .Where(e => e.Metadata.ClrType.Name == "GalleryImage")
                .ToList();

            foreach (var trackedImage in trackedGalleryImages)
            {
                var imageId = (Guid)trackedImage.Property("Id").CurrentValue!;

                // If this image is tracked but not in the current collection, mark it as deleted
                if (!currentGalleryIds.Contains(imageId))
                {
                    trackedImage.State = EntityState.Deleted;
                }
            }

            return Task.CompletedTask;
        }

        public async Task DeleteProviderAsync(Provider provider, CancellationToken cancellationToken = default)
        {
            await RemoveAsync(provider, cancellationToken);
        }
    }
}

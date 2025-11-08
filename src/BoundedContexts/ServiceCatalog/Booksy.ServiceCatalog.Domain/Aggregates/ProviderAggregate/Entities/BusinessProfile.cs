using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Entities
{
    /// <summary>
    /// Business profile information for a provider
    /// </summary>
    public sealed class BusinessProfile : Entity<Guid>
    {
        private readonly List<GalleryImage> _galleryImages = new();
        private const int MaxGalleryImages = 50;

        public string BusinessName { get; private set; }
        public string BusinessDescription { get; private set; }
        public string? Website { get; private set; }
        public string? LogoUrl { get; private set; }
        public string? ProfileImageUrl { get; private set; }
        public Dictionary<string, string> SocialMedia { get; private set; } = new();
        public List<string> Tags { get; private set; } = new();
        public DateTime LastUpdatedAt { get; private set; }

        // Gallery collection
        public IReadOnlyList<GalleryImage> GalleryImages => _galleryImages;

        // Private constructor for EF Core
        private BusinessProfile() : base()
        {
            BusinessName = string.Empty;
            BusinessDescription = string.Empty;
        }

        public static BusinessProfile Create(string businessName, string description, string? profileImageUrl)
        {
            return new BusinessProfile
            {
                Id = Guid.NewGuid(),
                BusinessName = businessName,
                ProfileImageUrl = profileImageUrl,
                BusinessDescription = description,
                LastUpdatedAt = DateTime.UtcNow
            };
        }

        public void UpdateLogo(string logoUrl)
        {
            LogoUrl = logoUrl;
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void UpdateProfileImage(string profileImageUrl)
        {
            ProfileImageUrl = profileImageUrl;
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void AddSocialMedia(string platform, string url)
        {
            SocialMedia[platform] = url;
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void RemoveSocialMedia(string platform)
        {
            SocialMedia.Remove(platform);
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void AddTag(string tag)
        {
            if (!Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                Tags.Add(tag);
                LastUpdatedAt = DateTime.UtcNow;
            }
        }

        public void RemoveTag(string tag)
        {
            Tags.RemoveAll(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
            LastUpdatedAt = DateTime.UtcNow;
        }

        // Gallery management methods
        public GalleryImage AddGalleryImage(
            ProviderId providerId,
            string imageUrl,
            string thumbnailUrl,
            string mediumUrl)
        {
            if (_galleryImages.Count(img => img.IsActive) >= MaxGalleryImages)
            {
                throw new DomainValidationException($"Cannot add more than {MaxGalleryImages} gallery images");
            }

            var nextOrder = _galleryImages.Count > 0
                ? _galleryImages.Max(img => img.DisplayOrder) + 1
                : 0;

            var galleryImage = GalleryImage.Create(
                providerId,
                imageUrl,
                thumbnailUrl,
                mediumUrl,
                nextOrder);

            _galleryImages.Add(galleryImage);
            LastUpdatedAt = DateTime.UtcNow;

            return galleryImage;
        }

        public void RemoveGalleryImage(Guid imageId)
        {
            var image = _galleryImages.FirstOrDefault(img => img.Id == imageId);
            if (image == null)
            {
                throw new DomainValidationException("Gallery image not found");
            }

            image.Deactivate();
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void ReorderGalleryImages(Dictionary<Guid, int> imageOrders)
        {
            foreach (var (imageId, newOrder) in imageOrders)
            {
                var image = _galleryImages.FirstOrDefault(img => img.Id == imageId);
                if (image == null)
                {
                    throw new DomainValidationException($"Gallery image {imageId} not found");
                }

                image.UpdateDisplayOrder(newOrder);
            }

            LastUpdatedAt = DateTime.UtcNow;
        }

        public GalleryImage? GetGalleryImage(Guid imageId)
        {
            return _galleryImages.FirstOrDefault(img => img.Id == imageId);
        }

        public void SetPrimaryGalleryImage(Guid imageId)
        {
            var image = _galleryImages.FirstOrDefault(img => img.Id == imageId && img.IsActive);
            if (image == null)
            {
                throw new DomainValidationException("Gallery image not found or inactive");
            }

            // Unset all other images as primary (only one can be primary)
            foreach (var existingImage in _galleryImages.Where(img => img.IsPrimary))
            {
                existingImage.UnsetAsPrimary();
            }

            // Set the new primary image
            image.SetAsPrimary();
            LastUpdatedAt = DateTime.UtcNow;
        }
    }
}
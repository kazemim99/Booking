
namespace Booksy.ServiceCatalog.Domain.Entities
{
    /// <summary>
    /// Business profile information for a provider
    /// </summary>
    public sealed class BusinessProfile : Entity<Guid>
    {
        public string BusinessName { get; private set; }
        public string BusinessDescription { get; private set; }
        public string? Website { get; private set; }
        public string? LogoUrl { get; private set; }
        public Dictionary<string, string> SocialMedia { get; private set; } = new();
        public List<string> Tags { get; private set; } = new();
        public DateTime LastUpdatedAt { get; private set; }

        // Private constructor for EF Core
        private BusinessProfile() : base()
        {
            BusinessName = string.Empty;
            BusinessDescription = string.Empty;
        }

        public static BusinessProfile Create(string businessName, string description, string? website = null)
        {
            return new BusinessProfile
            {
                Id = Guid.NewGuid(),
                BusinessName = businessName,
                BusinessDescription = description,
                Website = website,
                LastUpdatedAt = DateTime.UtcNow
            };
        }

        public void UpdateLogo(string logoUrl)
        {
            LogoUrl = logoUrl;
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
    }
}
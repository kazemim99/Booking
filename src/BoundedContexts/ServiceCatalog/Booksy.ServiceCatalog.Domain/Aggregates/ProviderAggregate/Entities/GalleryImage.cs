using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Entities;

/// <summary>
/// Gallery image entity for provider's image gallery
/// </summary>
public sealed class GalleryImage : Entity<Guid>
{
    public ProviderId ProviderId { get; private set; }
    public string ImageUrl { get; private set; }
    public string ThumbnailUrl { get; private set; }
    public string MediumUrl { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? Caption { get; private set; }
    public string? AltText { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsPrimary { get; private set; }

    // Private constructor for EF Core
    private GalleryImage() : base()
    {
        ImageUrl = string.Empty;
        ThumbnailUrl = string.Empty;
        MediumUrl = string.Empty;
    }

    public static GalleryImage Create(
        ProviderId providerId,
        string imageUrl,
        string thumbnailUrl,
        string mediumUrl,
        int displayOrder,
        bool isPrimary = false)
    {
        return new GalleryImage
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            ImageUrl = imageUrl,
            ThumbnailUrl = thumbnailUrl,
            MediumUrl = mediumUrl,
            DisplayOrder = displayOrder,
            UploadedAt = DateTime.UtcNow,
            IsActive = true,
            IsPrimary = isPrimary
        };
    }

    public void UpdateMetadata(string? caption, string? altText)
    {
        Caption = caption;
        AltText = altText;
    }

    public void UpdateDisplayOrder(int newOrder)
    {
        DisplayOrder = newOrder;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void UnsetAsPrimary()
    {
        IsPrimary = false;
    }
}

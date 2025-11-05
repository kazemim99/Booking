namespace Booksy.ServiceCatalog.API.Models.Responses;

public sealed class GalleryImageResponse
{
    public Guid Id { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string MediumUrl { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public DateTime UploadedAt { get; set; }
    public bool IsPrimary { get; set; }
}

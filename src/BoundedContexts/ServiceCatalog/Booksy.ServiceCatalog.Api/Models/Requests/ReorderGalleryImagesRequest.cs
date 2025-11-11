namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request to reorder gallery images and optionally set a primary image
/// </summary>
public sealed class ReorderGalleryImagesRequest
{
    /// <summary>
    /// Mapping of image IDs to their new display order
    /// </summary>
    public Dictionary<Guid, int> ImageOrders { get; set; } = new();

    /// <summary>
    /// Optional: ID of the image to set as primary
    /// </summary>
    public Guid? PrimaryImageId { get; set; }
}

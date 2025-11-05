namespace Booksy.ServiceCatalog.API.Models.Requests;

public sealed class ReorderGalleryImagesRequest
{
    public Dictionary<Guid, int> ImageOrders { get; set; } = new();
}

namespace Booksy.ServiceCatalog.API.Models.Requests;

public sealed class UpdateGalleryImageMetadataRequest
{
    public string? Caption { get; set; }
    public string? AltText { get; set; }
}

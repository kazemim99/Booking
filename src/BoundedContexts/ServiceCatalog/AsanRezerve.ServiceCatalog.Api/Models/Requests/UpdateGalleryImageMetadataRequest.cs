namespace AsanRezerve.ServiceCatalog.API.Models.Requests;

// Length limits are enforced by UpdateGalleryImageMetadataCommandValidator, not by DataAnnotations here:
// the host sets ApiBehaviorOptions.SuppressModelStateInvalidFilter = true, so attributes on request models
// are inert and validation runs through the MediatR ValidationBehavior against the command.
public sealed class UpdateGalleryImageMetadataRequest
{
    public string? Caption { get; set; }

    public string? AltText { get; set; }
}

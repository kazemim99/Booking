using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

/// <summary>
/// Step 7: Save gallery images to provider draft
/// Requires Step 3 to be completed (provider draft must exist)
/// Images are uploaded separately via the UploadGalleryImages endpoint
/// This command saves the references to already uploaded images
/// </summary>
public sealed record SaveStep7GalleryCommand(
    Guid ProviderId,
    List<string> ImageUrls, // URLs of already uploaded images
    Guid? IdempotencyKey = null
) : ICommand<SaveStep7GalleryResult>
{
    public Guid? PrimaryImageId { get;  set; }
}

public sealed record SaveStep7GalleryResult(
    Guid ProviderId,
    int RegistrationStep,
    int ImagesCount,
    string Message
);

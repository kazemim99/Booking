namespace Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

/// <summary>
/// Result for Step 7 gallery upload in registration flow
/// This is returned after successfully uploading gallery images during provider registration
/// </summary>
public sealed record SaveStep7GalleryResult(
    Guid ProviderId,
    int RegistrationStep,
    int ImagesCount,
    string Message
);

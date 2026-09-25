using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.UpdateGalleryImageMetadata;

public sealed record UpdateGalleryImageMetadataCommand(
    Guid ProviderId,
    Guid ImageId,
    string? Caption,
    string? AltText,
    Guid? IdempotencyKey = null) : ICommand;

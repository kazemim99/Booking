using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.DeleteGalleryImage;

public sealed record DeleteGalleryImageCommand(
    Guid ProviderId,
    Guid ImageId,
    Guid? IdempotencyKey = null) : ICommand;

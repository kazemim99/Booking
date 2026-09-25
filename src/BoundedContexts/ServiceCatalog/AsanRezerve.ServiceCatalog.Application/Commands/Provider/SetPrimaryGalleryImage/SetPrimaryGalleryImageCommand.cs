using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.SetPrimaryGalleryImage;

public sealed record SetPrimaryGalleryImageCommand(
    Guid ProviderId,
    Guid ImageId,
    Guid? IdempotencyKey = null) : ICommand;

using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.SetPrimaryGalleryImage;

public sealed record SetPrimaryGalleryImageCommand(
    Guid ProviderId,
    Guid ImageId,
    Guid? IdempotencyKey = null) : ICommand;

using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.DeleteGalleryImage;

public sealed record DeleteGalleryImageCommand(
    Guid ProviderId,
    Guid ImageId,
    Guid? IdempotencyKey = null) : ICommand;

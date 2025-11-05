using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.ReorderGalleryImages;

public sealed record ReorderGalleryImagesCommand(
    Guid ProviderId,
    Dictionary<Guid, int> ImageOrders,
    Guid? IdempotencyKey = null) : ICommand;

using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.ReorderGalleryImages;

/// <summary>
/// Reorder gallery images and optionally set a primary image
/// This allows updating both order and primary status in a single request
/// </summary>
public sealed record ReorderGalleryImagesCommand(
    Guid ProviderId,
    Dictionary<Guid, int> ImageOrders,
    Guid? PrimaryImageId = null, // Optional: Set this image as primary
    Guid? IdempotencyKey = null) : ICommand;

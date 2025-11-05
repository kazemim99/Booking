using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Microsoft.AspNetCore.Http;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UploadGalleryImages;

public sealed record UploadGalleryImagesCommand(
    Guid ProviderId,
    IFormFileCollection Images,
    Guid? IdempotencyKey = null) : ICommand<List<GalleryImageDto>>;

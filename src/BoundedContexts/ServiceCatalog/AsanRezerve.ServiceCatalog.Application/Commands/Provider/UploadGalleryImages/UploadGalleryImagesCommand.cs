using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Application.DTOs.Provider;
using Microsoft.AspNetCore.Http;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.UploadGalleryImages;

public sealed record UploadGalleryImagesCommand(
    Guid ProviderId,
    IFormFileCollection Images,
    Guid? IdempotencyKey = null) : ICommand<List<GalleryImageDto>>;

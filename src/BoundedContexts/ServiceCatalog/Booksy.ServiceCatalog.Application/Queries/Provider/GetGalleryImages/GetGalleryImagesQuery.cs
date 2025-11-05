using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.DTOs.Provider;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetGalleryImages;

public sealed record GetGalleryImagesQuery(Guid ProviderId) : IQuery<List<GalleryImageDto>>;

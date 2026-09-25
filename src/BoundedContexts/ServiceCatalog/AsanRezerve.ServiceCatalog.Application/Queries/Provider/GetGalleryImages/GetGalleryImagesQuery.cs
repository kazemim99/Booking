using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Application.DTOs.Provider;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetGalleryImages;

public sealed record GetGalleryImagesQuery(Guid ProviderId) : IQuery<List<GalleryImageDto>>;

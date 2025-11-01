using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetGalleryImages;

public sealed class GetGalleryImagesQueryHandler
    : IQueryHandler<GetGalleryImagesQuery, List<GalleryImageDto>>
{
    private readonly IProviderReadRepository _providerRepository;

    public GetGalleryImagesQueryHandler(IProviderReadRepository providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<List<GalleryImageDto>> Handle(
        GetGalleryImagesQuery request,
        CancellationToken cancellationToken)
    {
        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
        {
            return new List<GalleryImageDto>();
        }

        return provider.Profile.GalleryImages
            .Where(img => img.IsActive)
            .OrderBy(img => img.DisplayOrder)
            .Select(img => new GalleryImageDto
            {
                Id = img.Id,
                ThumbnailUrl = img.ThumbnailUrl,
                MediumUrl = img.MediumUrl,
                OriginalUrl = img.ImageUrl,
                DisplayOrder = img.DisplayOrder,
                Caption = img.Caption,
                AltText = img.AltText,
                UploadedAt = img.UploadedAt,
                IsActive = img.IsActive
            })
            .ToList();
    }
}

using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Application.Abstractions;
using AsanRezerve.ServiceCatalog.Application.DTOs.Provider;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetGalleryImages;

public sealed class GetGalleryImagesQueryHandler
    : IQueryHandler<GetGalleryImagesQuery, List<GalleryImageDto>>
{
    private readonly IProviderReadRepository _providerRepository;
    private readonly IUrlService _urlService;

    public GetGalleryImagesQueryHandler(
        IProviderReadRepository providerRepository,
        IUrlService urlService)
    {
        _providerRepository = providerRepository;
        _urlService = urlService;
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

        var response = provider.Profile.GalleryImages
            .Where(img => img.IsActive)
            .OrderByDescending(img => img.IsPrimary)
            .ThenBy(img => img.DisplayOrder)
            .Select(img => new GalleryImageDto
            {
                Id = img.Id,
                ThumbnailUrl = _urlService.ToAbsoluteUrl(img.ThumbnailUrl),
                MediumUrl = _urlService.ToAbsoluteUrl(img.MediumUrl),
                OriginalUrl = _urlService.ToAbsoluteUrl(img.ImageUrl),
                DisplayOrder = img.DisplayOrder,
                Caption = img.Caption,
                AltText = img.AltText,
                UploadedAt = img.UploadedAt,
                IsActive = img.IsActive,
                IsPrimary = img.IsPrimary
            })
            .ToList();
        return response;
    }
}

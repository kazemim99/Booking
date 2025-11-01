using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.Services;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UploadGalleryImages;

public sealed class UploadGalleryImagesCommandHandler
    : ICommandHandler<UploadGalleryImagesCommand, List<GalleryImageDto>>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public UploadGalleryImagesCommandHandler(
        IProviderWriteRepository providerRepository,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork)
    {
        _providerRepository = providerRepository;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<GalleryImageDto>> Handle(
        UploadGalleryImagesCommand request,
        CancellationToken cancellationToken)
    {
        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
        {
            throw new DomainValidationException($"Provider {request.ProviderId} not found");
        }

        var uploadedImages = new List<GalleryImageDto>();

        foreach (var file in request.Images)
        {
            if (file.Length == 0)
                continue;

            // Upload and optimize image
            using var stream = file.OpenReadStream();
            var storageResult = await _fileStorageService.UploadImageAsync(
                request.ProviderId,
                stream,
                file.FileName,
                cancellationToken);

            // Add to domain
            var galleryImage = provider.Profile.AddGalleryImage(
                providerId,
                storageResult.OriginalUrl,
                storageResult.ThumbnailUrl,
                storageResult.MediumUrl);

            uploadedImages.Add(new GalleryImageDto
            {
                Id = galleryImage.Id,
                ThumbnailUrl = galleryImage.ThumbnailUrl,
                MediumUrl = galleryImage.MediumUrl,
                OriginalUrl = galleryImage.ImageUrl,
                DisplayOrder = galleryImage.DisplayOrder,
                Caption = galleryImage.Caption,
                AltText = galleryImage.AltText,
                UploadedAt = galleryImage.UploadedAt,
                IsActive = galleryImage.IsActive
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return uploadedImages;
    }
}

using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.Services;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.DeleteGalleryImage;

public sealed class DeleteGalleryImageCommandHandler
    : ICommandHandler<DeleteGalleryImageCommand>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IFileStorageService _fileStorageService;

    public DeleteGalleryImageCommandHandler(
        IProviderWriteRepository providerRepository,
        IFileStorageService fileStorageService)
    {
        _providerRepository = providerRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task Handle(
        DeleteGalleryImageCommand request,
        CancellationToken cancellationToken)
    {
        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
        {
            throw new DomainValidationException($"Provider {request.ProviderId} not found");
        }

        var image = provider.Profile.GetGalleryImage(request.ImageId);
        if (image == null)
        {
            throw new DomainValidationException($"Gallery image {request.ImageId} not found");
        }

        // Store image URLs before removal (needed for file deletion)
        var thumbnailUrl = image.ThumbnailUrl;
        var mediumUrl = image.MediumUrl;
        var originalUrl = image.ImageUrl;

        // Hard delete - remove from domain collection (this raises GalleryImageDeletedEvent for cache invalidation)
        provider.DeleteGalleryImage(request.ImageId);

        // Mark the provider as modified to ensure EF Core detects the change
        // For owned collections, we need to explicitly tell the repository to update
        await _providerRepository.UpdateProviderAsync(provider, cancellationToken);

        // Delete files asynchronously (best effort)
        await _fileStorageService.DeleteImageAsync(thumbnailUrl, cancellationToken);
        await _fileStorageService.DeleteImageAsync(mediumUrl, cancellationToken);
        await _fileStorageService.DeleteImageAsync(originalUrl, cancellationToken);
    }
}

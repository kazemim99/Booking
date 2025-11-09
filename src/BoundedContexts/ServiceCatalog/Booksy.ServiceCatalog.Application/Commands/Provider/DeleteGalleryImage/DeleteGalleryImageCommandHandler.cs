using Booksy.Core.Application.Abstractions.CQRS;
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

        // Soft delete in domain
        provider.Profile.RemoveGalleryImage(request.ImageId);

        // Mark entity as modified for EF Core change tracking
        await _providerRepository.SaveAsync(provider, cancellationToken);

        // Delete files asynchronously (best effort)
        await _fileStorageService.DeleteImageAsync(image.ThumbnailUrl, cancellationToken);
        await _fileStorageService.DeleteImageAsync(image.MediumUrl, cancellationToken);
        await _fileStorageService.DeleteImageAsync(image.ImageUrl, cancellationToken);
    }
}

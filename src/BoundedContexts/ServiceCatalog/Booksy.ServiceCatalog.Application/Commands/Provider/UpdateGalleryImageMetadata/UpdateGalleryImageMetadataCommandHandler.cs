using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateGalleryImageMetadata;

public sealed class UpdateGalleryImageMetadataCommandHandler
    : ICommandHandler<UpdateGalleryImageMetadataCommand>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;

    public UpdateGalleryImageMetadataCommandHandler(
        IProviderWriteRepository providerRepository,
        IServiceCatalogUnitOfWork unitOfWork)
    {
        _providerRepository = providerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        UpdateGalleryImageMetadataCommand request,
        CancellationToken cancellationToken)
    {
        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
        {
            // DomainValidationException, matching DeleteGalleryImageCommandHandler: the middleware maps it
            // to a clean 4xx, whereas InvalidOperationException escaped as a 500 for a plainly-addressable
            // client error.
            throw new DomainValidationException($"Provider {request.ProviderId} not found");
        }

        if (provider.Profile.GetGalleryImage(request.ImageId) is null)
        {
            throw new DomainValidationException($"Gallery image {request.ImageId} not found");
        }

        // Route the edit through BusinessProfile rather than mutating the child directly, so
        // Profile.LastUpdatedAt advances with every meaningful gallery change. Returns false for a
        // no-op edit, in which case there is nothing to persist.
        var changed = provider.Profile.UpdateGalleryImageMetadata(
            request.ImageId, request.Caption, request.AltText);

        if (!changed)
        {
            return;
        }

        // This handler previously mutated the image and returned: _unitOfWork was injected but never
        // used and the repository was never told the aggregate had changed, so caption/alt-text edits
        // were silently discarded. Mirrors DeleteGalleryImageCommandHandler, which does both because
        // EF needs the owned collection's parent explicitly marked modified.
        await _providerRepository.UpdateProviderAsync(provider, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

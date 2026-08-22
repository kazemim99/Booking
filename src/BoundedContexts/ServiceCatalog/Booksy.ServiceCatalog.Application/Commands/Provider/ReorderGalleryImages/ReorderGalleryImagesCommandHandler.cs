using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.ReorderGalleryImages;

public sealed class ReorderGalleryImagesCommandHandler
    : ICommandHandler<ReorderGalleryImagesCommand>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;

    public ReorderGalleryImagesCommandHandler(
        IProviderWriteRepository providerRepository,
        IServiceCatalogUnitOfWork unitOfWork)
    {
        _providerRepository = providerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        ReorderGalleryImagesCommand request,
        CancellationToken cancellationToken)
    {
        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
        {
            throw new InvalidOperationException($"Provider {request.ProviderId} not found");
        }

        // Reorder images
        provider.Profile.ReorderGalleryImages(request.ImageOrders);

        // Set primary image if specified
        if (request.PrimaryImageId.HasValue)
        {
            provider.Profile.SetPrimaryGalleryImage(request.PrimaryImageId.Value);
        }

        // The unit of work was previously a constructor parameter that was never even assigned to a
        // field, so a reorder mutated the in-memory aggregate and was then discarded — the GET that
        // followed returned the original order. Same defect family as
        // UpdateGalleryImageMetadataCommandHandler. UpdateProviderAsync is required in addition to
        // SaveChangesAsync because EF needs the owned collection's parent explicitly marked modified.
        await _providerRepository.UpdateProviderAsync(provider, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

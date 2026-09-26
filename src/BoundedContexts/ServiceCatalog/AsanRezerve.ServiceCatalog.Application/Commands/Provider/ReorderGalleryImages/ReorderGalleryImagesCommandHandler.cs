using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.ReorderGalleryImages;

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

        // Go through the aggregate root, not provider.Profile directly: the root raises the domain events.
        // History: a mutation applied straight to Profile once persisted correctly while a provider cache kept
        // serving the pre-change gallery, because that cache was invalidated by these events only. Cached salon
        // reads are now evicted by the save pipeline for any provider change (add-observability-and-caching).
        provider.ReorderGalleryImages(request.ImageOrders);

        if (request.PrimaryImageId.HasValue)
        {
            provider.SetPrimaryGalleryImage(request.PrimaryImageId.Value);
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

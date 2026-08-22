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

        // Go through the aggregate root, not provider.Profile directly. The root's methods raise the
        // domain events that ProviderCacheInvalidationEventHandler listens for, and the read path is
        // decorated by CachedProviderReadRepository — so a mutation applied straight to Profile persists
        // correctly but leaves the cache holding the pre-change gallery. That was the actual defect here:
        // raw SQL confirmed display_order was written exactly as requested, while the API kept serving
        // the stale snapshot. DeleteGalleryImage never had the bug because it already routed through the
        // root.
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

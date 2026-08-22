using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.SetPrimaryGalleryImage;

public sealed class SetPrimaryGalleryImageCommandHandler
    : ICommandHandler<SetPrimaryGalleryImageCommand>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;

    public SetPrimaryGalleryImageCommandHandler(
        IProviderWriteRepository providerRepository,
        IServiceCatalogUnitOfWork unitOfWork)
    {
        _providerRepository = providerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        SetPrimaryGalleryImageCommand request,
        CancellationToken cancellationToken)
    {
        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
        {
            throw new DomainValidationException($"Provider {request.ProviderId} not found");
        }

        // Through the aggregate root so the cache-invalidation domain event is raised; see the note in
        // ReorderGalleryImagesCommandHandler.
        provider.SetPrimaryGalleryImage(request.ImageId);

        // UpdateProviderAsync as well as SaveChangesAsync: EF needs the owned collection's parent
        // explicitly marked modified for changes to the child images to be detected, which is why
        // DeleteGalleryImageCommandHandler does both.
        await _providerRepository.UpdateProviderAsync(provider, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

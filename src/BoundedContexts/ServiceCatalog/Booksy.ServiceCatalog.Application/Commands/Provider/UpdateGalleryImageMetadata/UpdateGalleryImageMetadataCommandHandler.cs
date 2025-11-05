using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateGalleryImageMetadata;

public sealed class UpdateGalleryImageMetadataCommandHandler
    : ICommandHandler<UpdateGalleryImageMetadataCommand>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGalleryImageMetadataCommandHandler(
        IProviderWriteRepository providerRepository,
        IUnitOfWork unitOfWork)
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
            throw new InvalidOperationException($"Provider {request.ProviderId} not found");
        }

        var image = provider.Profile.GetGalleryImage(request.ImageId);
        if (image == null)
        {
            throw new InvalidOperationException($"Gallery image {request.ImageId} not found");
        }

        image.UpdateMetadata(request.Caption, request.AltText);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

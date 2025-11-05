using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.ReorderGalleryImages;

public sealed class ReorderGalleryImagesCommandHandler
    : ICommandHandler<ReorderGalleryImagesCommand>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderGalleryImagesCommandHandler(
        IProviderWriteRepository providerRepository,
        IUnitOfWork unitOfWork)
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

        provider.Profile.ReorderGalleryImages(request.ImageOrders);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

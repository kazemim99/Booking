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
    private readonly IUnitOfWork _unitOfWork;

    public SetPrimaryGalleryImageCommandHandler(
        IProviderWriteRepository providerRepository,
        IUnitOfWork unitOfWork)
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

        provider.Profile.SetPrimaryGalleryImage(request.ImageId);

    }
}

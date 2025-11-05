using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

public sealed class SaveStep7GalleryCommandHandler
    : ICommandHandler<SaveStep7GalleryCommand, SaveStep7GalleryResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SaveStep7GalleryCommandHandler(
        IProviderWriteRepository providerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _providerRepository = providerRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<SaveStep7GalleryResult> Handle(
        SaveStep7GalleryCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(_currentUserService.UserId ??
            throw new UnauthorizedAccessException("User not authenticated"));

        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
            throw new InvalidOperationException("Provider not found");

        if (provider.OwnerId != userId)
            throw new UnauthorizedAccessException("You are not authorized to update this provider");

        if (provider.Status != ProviderStatus.Drafted)
            throw new InvalidOperationException("Provider is not in draft status");

        // Gallery images are already uploaded via UploadGalleryImages endpoint
        // This step just marks the gallery step as complete
        // Provider aggregate doesn't store gallery images directly - they're in a separate aggregate

        // Update registration step
        provider.UpdateRegistrationStep(7);

        await _unitOfWork.CommitAsync(cancellationToken);

        // Image count from request since Provider doesn't have GalleryImages property
        var imageCount = request.ImageUrls?.Count ?? 0;

        return new SaveStep7GalleryResult(
            provider.Id.Value,
            7,
            imageCount,
            $"Gallery step completed. {imageCount} image(s) uploaded.");
    }
}

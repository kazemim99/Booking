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

public sealed class SaveStep9CompleteCommandHandler
    : ICommandHandler<SaveStep9CompleteCommand, SaveStep9CompleteResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IServiceWriteRepository _serviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SaveStep9CompleteCommandHandler(
        IProviderWriteRepository providerRepository,
        IServiceWriteRepository serviceRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _providerRepository = providerRepository;
        _serviceRepository = serviceRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<SaveStep9CompleteResult> Handle(
        SaveStep9CompleteCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(_currentUserService.UserId ??
            throw new UnauthorizedAccessException("User not authenticated"));

        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
            throw new InvalidOperationException("Provider not found");

        if (provider.OwnerId != userId)
            throw new UnauthorizedAccessException("You are not authorized to complete this registration");

        if (provider.Status != ProviderStatus.Drafted)
            throw new InvalidOperationException("Provider is not in draft status");

        // Validate required data
        if (!provider.BusinessHours.Any())
            throw new InvalidOperationException("Business hours are required to complete registration");

        // Check services from ServiceRepository (Service is separate aggregate)
        var services = await _serviceRepository.GetServicesByProviderIdAsync(providerId, cancellationToken);
        if (!services.Any())
            throw new InvalidOperationException("At least one service is required to complete registration");

        if (string.IsNullOrWhiteSpace(provider.Profile.BusinessName))
            throw new InvalidOperationException("Business name is required");

        if (provider.Address == null)
            throw new InvalidOperationException("Business address is required");

        if (provider.ContactInfo?.Email == null)
            throw new InvalidOperationException("Contact email is required");

        // Complete registration - transitions to PendingVerification
        provider.CompleteRegistration();
        provider.UpdateRegistrationStep(9);

        await _unitOfWork.CommitAsync(cancellationToken);

        return new SaveStep9CompleteResult(
            ProviderId: provider.Id.Value,
            Status: provider.Status.ToString(),
            Message: "Registration completed successfully. Your provider profile is now pending admin verification.",
            AccessToken: null, // Tokens are handled by authentication service
            RefreshToken: null
        );
    }
}

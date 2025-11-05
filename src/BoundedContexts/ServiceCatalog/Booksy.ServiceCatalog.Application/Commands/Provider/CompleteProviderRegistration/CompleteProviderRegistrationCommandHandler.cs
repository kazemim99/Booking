using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.CompleteProviderRegistration;

public sealed class CompleteProviderRegistrationCommandHandler
    : ICommandHandler<CompleteProviderRegistrationCommand, CompleteProviderRegistrationResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CompleteProviderRegistrationCommandHandler(
        IProviderWriteRepository providerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _providerRepository = providerRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CompleteProviderRegistrationResult> Handle(
        CompleteProviderRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Get current user ID
        var userId = UserId.From(_currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated"));

        // 2. Get provider by ID
        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
        {
            throw new InvalidOperationException("Provider not found");
        }

        // 3. Verify ownership
        if (provider.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to complete this registration");
        }

        // 4. Verify provider is in draft status
        if (provider.Status != ProviderStatus.Drafted)
        {
            throw new InvalidOperationException("Provider is not in draft status");
        }

        // 5. Validate that required data is present
        // Business Hours - at least one day must be open
        if (!provider.BusinessHours.Any())
        {
            throw new InvalidOperationException("Business hours are required");
        }

        // Services - at least one service required
        if (!provider.Services.Any())
        {
            throw new InvalidOperationException("At least one service is required");
        }

        // 6. Complete registration
        provider.CompleteRegistration();

        // 7. Save
        await _unitOfWork.CommitAsync(cancellationToken);

        // 8. Return success result
        // Note: Frontend should re-authenticate or fetch updated user claims to get provider role
        return new CompleteProviderRegistrationResult(
            ProviderId: provider.Id.Value,
            Status: provider.Status.ToString(),
            Message: "Registration completed successfully. Pending admin approval.",
            AccessToken: null, // Frontend will handle token refresh
            RefreshToken: null
        );
    }
}

using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.CreateProviderDraft;

public sealed class CreateProviderDraftCommandHandler
    : ICommandHandler<CreateProviderDraftCommand, CreateProviderDraftResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateProviderDraftCommandHandler(
        IProviderWriteRepository providerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _providerRepository = providerRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CreateProviderDraftResult> Handle(
        CreateProviderDraftCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Get current user ID
        var userId = UserId.From(_currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated"));

        // 2. Check if user already has a draft provider
        var existingProvider = await _providerRepository
            .GetDraftProviderByOwnerIdAsync(userId, cancellationToken);

        if (existingProvider != null)
        {
            // Return existing draft
            return new CreateProviderDraftResult(
                existingProvider.Id.Value,
                existingProvider.RegistrationStep,
                "Draft provider already exists. Resuming registration.");
        }

        // 3. Map category string to ProviderType enum
        if (!Enum.TryParse<ProviderType>(request.Category, true, out var providerType))
        {
            throw new InvalidOperationException($"Invalid category: {request.Category}");
        }

        // 4. Create value objects
        var contactInfo = ContactInfo.Create(
            Email.Create(request.Email),
            PhoneNumber.Create(request.PhoneNumber));

        // Combine address lines into street
        var street = string.IsNullOrWhiteSpace(request.AddressLine2)
            ? request.AddressLine1
            : $"{request.AddressLine1}, {request.AddressLine2}";

        var formattedAddress = $"{street}, {request.City}, {request.Province}";

        var address = BusinessAddress.Create(
            formattedAddress,
            street,
            request.City,
            request.Province,
            request.PostalCode,
            "IR", // Default to Iran
            null, // ProvinceId
            null, // CityId
            (double)request.Latitude,
            (double)request.Longitude);

        // 5. Create draft provider
        // Note: This is a legacy endpoint. Owner names are not collected here.
        // Use SaveStep3LocationCommand from ProviderRegistrationController for the full registration flow.
        var provider = Domain.Aggregates.Provider.CreateDraft(
            userId,
            ownerFirstName: string.Empty, // Legacy endpoint - no owner name collected
            ownerLastName: string.Empty,  // Legacy endpoint - no owner name collected
            request.BusinessName,
            request.BusinessDescription,
            providerType,
            contactInfo,
            address,
            registrationStep: 3); // Created after completing Step 3

        // 6. Save
        await _providerRepository.SaveProviderAsync(provider, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new CreateProviderDraftResult(
            provider.Id.Value,
            provider.RegistrationStep,
            "Draft provider created successfully");
    }
}

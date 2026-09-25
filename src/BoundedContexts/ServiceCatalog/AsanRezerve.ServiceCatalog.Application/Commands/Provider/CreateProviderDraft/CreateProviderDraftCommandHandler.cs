using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Application.Abstractions.Services;
using AsanRezerve.Core.Domain.Abstractions;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Application.Common;
using AsanRezerve.ServiceCatalog.Application.Services.Interfaces;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.CreateProviderDraft;

public sealed class CreateProviderDraftCommandHandler
    : ICommandHandler<CreateProviderDraftCommand, CreateProviderDraftResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPersonAccountProvisioningService _personAccounts;

    public CreateProviderDraftCommandHandler(
        IProviderWriteRepository providerRepository,
        IServiceCatalogUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPersonAccountProvisioningService personAccounts)
    {
        _providerRepository = providerRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _personAccounts = personAccounts;
    }

    /// <summary>
    /// The owner's account keeps the placeholder phone sign-in gave it («ارائه‌دهنده 9123135143»)
    /// unless someone replaces it — and this is where the real name first arrives. Without it the
    /// staff picker listed the owner under the placeholder (2026-09-19). Best effort by contract.
    /// </summary>
    private Task AdoptOwnerNameAsync(UserId owner, CreateProviderDraftCommand request, CancellationToken ct) =>
        _personAccounts.AdoptNameIfPlaceholderAsync(owner.Value, request.OwnerFirstName, request.OwnerLastName, ct);

    public async Task<CreateProviderDraftResult> Handle(
        CreateProviderDraftCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Get current user ID
        var userId = UserId.From(_currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated"));

        // 2. Map category string to ServiceCategory enum
        // Resolved centrally: accepts the enum name, the numeric id, the category slug and the
        // wizard's legacy taxonomy ids, and rejects anything else. A bare Enum.TryParse used to
        // accept any number here, letting an undefined category reach the aggregate.
        if (!ServiceCategoryResolver.TryResolve(request.Category, out var category))
        {
            // The caller sent a category the system does not have; that is a 400, and the action
            // documents one. InvalidOperationException is unmapped by ExceptionHandlingMiddleware,
            // so a mistyped category came back as a 500 with no usable message.
            throw new DomainValidationException(nameof(request.Category), $"Invalid category: {request.Category}");
        }

        // 3. Create value objects
        var contactInfo = ContactInfo.Create(
            Email.Create(request.Email),
            PhoneNumber.From(request.PhoneNumber));

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

        // 4. Check if user already has a draft provider
        var existingProvider = await _providerRepository
            .GetDraftProviderByOwnerIdAsync(userId, cancellationToken);

        Domain.Aggregates.Provider provider;

        if (existingProvider != null)
        {
            // Update existing draft with new information
            existingProvider.UpdateDraftInfo(
                request.OwnerFirstName,
                request.OwnerLastName,
                request.BusinessName,
                request.BusinessDescription,
                category,
                contactInfo,
                address,
                request.LogoUrl);

            provider = existingProvider;
            await _providerRepository.UpdateProviderAsync(provider, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            await AdoptOwnerNameAsync(userId, request, cancellationToken);

            return new CreateProviderDraftResult(
                provider.Id.Value,
                provider.RegistrationStep,
                "A draft provider already exists for this user; its details were updated",
                IsNewDraft: false);
        }

        // 5. Create new draft provider
        provider = Domain.Aggregates.Provider.CreateDraft(
            userId,
            request.OwnerFirstName,
            request.OwnerLastName,
            request.BusinessName,
            request.BusinessDescription,
            category,
            contactInfo,
            address,
            registrationStep: 3,
            logoUrl: request.LogoUrl);

        // 6. Save
        await _providerRepository.SaveProviderAsync(provider, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        await AdoptOwnerNameAsync(userId, request, cancellationToken);

        return new CreateProviderDraftResult(
            provider.Id.Value,
            provider.RegistrationStep,
            "Draft provider created successfully",
            IsNewDraft: true);
    }
}

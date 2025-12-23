using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Repositories;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetDraftProvider;

public sealed class GetDraftProviderQueryHandler
    : IQueryHandler<GetDraftProviderQuery, GetDraftProviderResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetDraftProviderQueryHandler(
        IProviderWriteRepository providerRepository,
        ICurrentUserService currentUserService)
    {
        _providerRepository = providerRepository;
        _currentUserService = currentUserService;
    }

    public async Task<GetDraftProviderResult> Handle(
        GetDraftProviderQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Get current user ID
        var userId = UserId.From(_currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated"));

        // 2. Check if user has a draft provider
        var draftProvider = await _providerRepository
            .GetDraftProviderByOwnerIdAsync(userId, cancellationToken);

        if (draftProvider == null)
        {
            return new GetDraftProviderResult(
                ProviderId: null,
                RegistrationStep: null,
                HasDraft: false,
                DraftData: null
            );
        }

        // 3. Map to result DTO
        var draftData = new DraftProviderData(
            ProviderId: draftProvider.Id.Value,
            BusinessName: draftProvider.Profile.BusinessName,
            BusinessDescription: draftProvider.Profile.BusinessDescription,
            Category: draftProvider.PrimaryCategory.ToString(),
            PhoneNumber: draftProvider.ContactInfo.PrimaryPhone?.Value ?? "",
            Email: draftProvider.ContactInfo.Email?.Value ?? "",
            AddressLine1: draftProvider.Address.Street,
            AddressLine2: null, // BusinessAddress doesn't have AddressLine2
            City: draftProvider.Address.City,
            Province: draftProvider.Address.State,
            PostalCode: draftProvider.Address.PostalCode,
            Latitude: (decimal)(draftProvider.Address.Latitude ?? 0),
            Longitude: (decimal)(draftProvider.Address.Longitude ?? 0),
            RegistrationStep: draftProvider.RegistrationStep
        );

        return new GetDraftProviderResult(
            ProviderId: draftProvider.Id.Value,
            RegistrationStep: draftProvider.RegistrationStep,
            HasDraft: true,
            DraftData: draftData
        );
    }
}

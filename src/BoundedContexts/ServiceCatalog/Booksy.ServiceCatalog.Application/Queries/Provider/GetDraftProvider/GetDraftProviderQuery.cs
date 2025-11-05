using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetDraftProvider;

public sealed record GetDraftProviderQuery() : IQuery<GetDraftProviderResult>;

public sealed record GetDraftProviderResult(
    Guid? ProviderId,
    int? RegistrationStep,
    bool HasDraft,
    DraftProviderData? DraftData
);

public sealed record DraftProviderData(
    Guid ProviderId,
    string BusinessName,
    string BusinessDescription,
    string Category,
    string PhoneNumber,
    string Email,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Province,
    string PostalCode,
    decimal Latitude,
    decimal Longitude,
    int RegistrationStep
);

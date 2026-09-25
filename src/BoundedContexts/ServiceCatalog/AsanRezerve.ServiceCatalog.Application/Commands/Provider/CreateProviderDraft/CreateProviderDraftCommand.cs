using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.CreateProviderDraft;

public sealed record CreateProviderDraftCommand(
    string BusinessName,
    string BusinessDescription,
    string Category,
    string PhoneNumber,
    string Email,
    string OwnerFirstName,
    string OwnerLastName,
    string? LogoUrl,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Province,
    string PostalCode,
    decimal Latitude,
    decimal Longitude,
    Guid? IdempotencyKey = null
) : ICommand<CreateProviderDraftResult>;

public sealed record CreateProviderDraftResult(
    Guid ProviderId,
    int RegistrationStep,
    string Message,
    /// <summary>
    /// True when this call created the draft, false when it updated one the user already had.
    /// The controller answers 201 or 200 on this. It used to decide by searching <see cref="Message"/>
    /// for "already exists" — which the handler never says — so an update was reported as a creation.
    /// </summary>
    bool IsNewDraft
);

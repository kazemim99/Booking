using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.CreateProviderDraft;

public sealed record CreateProviderDraftCommand(
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
    Guid? IdempotencyKey = null
) : ICommand<CreateProviderDraftResult>;

public sealed record CreateProviderDraftResult(
    Guid ProviderId,
    int RegistrationStep,
    string Message
);

using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.CompleteProviderRegistration;

public sealed record CompleteProviderRegistrationCommand(
    Guid ProviderId,
    Guid? IdempotencyKey = null
) : ICommand<CompleteProviderRegistrationResult>;

public sealed record CompleteProviderRegistrationResult(
    Guid ProviderId,
    string Status,
    string Message,
    string? AccessToken,
    string? RefreshToken
);

using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

/// <summary>
/// Step 9: Complete provider registration
/// Validates all required data and transitions provider to PendingVerification status
/// </summary>
public sealed record SaveStep9CompleteCommand(
    Guid ProviderId,
    Guid? IdempotencyKey = null
) : ICommand<SaveStep9CompleteResult>;

public sealed record SaveStep9CompleteResult(
    Guid ProviderId,
    string Status,
    string Message,
    string? AccessToken,
    string? RefreshToken
);

using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateProviderProfile;

/// <summary>
/// Command to update provider profile information
/// </summary>
public sealed record UpdateProviderProfileCommand(
    Guid ProviderId,
    string? FullName,
    string? Email,
    string? ProfileImageUrl,
    Guid? IdempotencyKey = null) : ICommand<UpdateProviderProfileResult>;

public sealed record UpdateProviderProfileResult(
    bool Success,
    string Message);

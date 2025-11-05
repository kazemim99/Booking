using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessLogo;

/// <summary>
/// Command to update provider business logo
/// </summary>
public sealed record UpdateBusinessLogoCommand(
    Guid ProviderId,
    string LogoUrl,
    Guid? IdempotencyKey = null) : ICommand<UpdateBusinessLogoResult>;

public sealed record UpdateBusinessLogoResult(
    bool Success,
    string Message);

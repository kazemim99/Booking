using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessInfo;

/// <summary>
/// Command to update provider business information
/// </summary>
public sealed record UpdateBusinessInfoCommand(
    Guid ProviderId,
    string BusinessName,
    string? Description,
    string OwnerFirstName,
    string OwnerLastName,
    string PhoneNumber,
    string? Email,
    string? Website,
    Guid? IdempotencyKey = null) : ICommand<UpdateBusinessInfoResult>;

public sealed record UpdateBusinessInfoResult(
    Guid ProviderId,
    string BusinessName,
    string OwnerFirstName,
    string OwnerLastName,
    DateTime UpdatedAt);

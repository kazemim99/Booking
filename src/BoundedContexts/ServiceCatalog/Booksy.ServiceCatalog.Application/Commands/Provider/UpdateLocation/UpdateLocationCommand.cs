using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateLocation;

public sealed record UpdateLocationCommand(
    Guid ProviderId,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country,
    double? Latitude,
    double? Longitude,
    string? FormattedAddress,
    bool IsShared,
    Guid? IdempotencyKey = null) : ICommand<UpdateLocationResult>;

public sealed record UpdateLocationResult(
    Guid ProviderId,
    string AddressLine1,
    string City,
    string State,
    string PostalCode,
    string Country,
    double? Latitude,
    double? Longitude,
    DateTime UpdatedAt)
{
}

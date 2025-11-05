using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateLocation;

public sealed record UpdateLocationCommand(
    Guid ProviderId,
    string FormattedAddress,
    string? AddressLine1,
    string? City,
    string? PostalCode,
    string Country,
    int? ProvinceId,
    int? CityId,
    double Latitude,
    double Longitude,
    Guid? IdempotencyKey = null) : ICommand<UpdateLocationResult>;

public sealed record UpdateLocationResult(
    Guid ProviderId,
    string FormattedAddress,
    string? AddressLine1,
    string? City,
    string? PostalCode,
    string Country,
    int? ProvinceId,
    int? CityId,
    double Latitude,
    double Longitude,
    DateTime UpdatedAt)
{
}

using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProvidersByLocation
{
    public sealed record GetProvidersByLocationQuery(
        double Latitude,
        double Longitude,
        double RadiusKm,
        ProviderType? ProviderType = null,
        bool? OffersMobileServices = null,
        int? MaxResults = 50) : IQuery<IReadOnlyList<ProviderLocationViewModel>>;
}
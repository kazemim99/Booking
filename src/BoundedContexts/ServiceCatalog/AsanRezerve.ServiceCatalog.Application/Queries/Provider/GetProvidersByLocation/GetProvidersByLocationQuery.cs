using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.CQRS;
using AsanRezerve.Core.Application.DTOs;
using AsanRezerve.ServiceCatalog.Application.DTOs.Provider;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetProvidersByLocation
{
    public sealed record GetProvidersByLocationQuery(
        double Latitude,
        double Longitude,
        double RadiusKm = 10.0,
        ServiceCategory? Category = null,
        bool? OffersMobileServices = null,
        int PageNumber = 1,
        int PageSize = 20) : PaginatedQueryBase<ProviderLocationItem>;
}
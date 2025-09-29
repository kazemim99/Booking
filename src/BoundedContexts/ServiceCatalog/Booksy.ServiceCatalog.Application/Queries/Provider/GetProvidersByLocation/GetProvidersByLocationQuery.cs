using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.CQRS;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Application.DTOs.Provider;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProvidersByLocation
{
    public sealed record GetProvidersByLocationQuery(
        double Latitude,
        double Longitude,
        double RadiusKm = 10.0,
        BusinessSize? Type = null,
        bool? OffersMobileServices = null,
        int PageNumber = 1,
        int PageSize = 20) : PaginatedQueryBase<ProviderLocationItem>;
}
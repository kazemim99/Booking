using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.CQRS;
using AsanRezerve.Core.Application.DTOs;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Service.SearchServices
{
    public sealed record SearchServicesQuery(
        string? SearchTerm = null,
        string? Category = null,
        ServiceType? Type = null,
        decimal? MinPrice = null,
        decimal? MaxPrice = null,
        int? MaxDurationMinutes = null,
        bool? AvailableAsMobile = null,
        string? City = null,
        string? State = null) : PaginatedQueryBase<ServiceSearchItem>;
}

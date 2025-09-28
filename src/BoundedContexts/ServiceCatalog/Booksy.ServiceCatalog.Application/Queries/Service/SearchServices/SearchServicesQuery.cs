using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.CQRS;
using Booksy.Core.Application.DTOs;

namespace Booksy.ServiceCatalog.Application.Queries.Service.SearchServices
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

// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/SearchProviders/SearchProvidersQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.SearchProviders
{
    public sealed record SearchProvidersQuery(
        string SearchTerm,
        ProviderType? ProviderType = null,
        string? City = null,
        string? State = null,
        bool? OffersMobileServices = null,
        int? MaxResults = 50) : IQuery<IReadOnlyList<ProviderSearchResultViewModel>>;
}
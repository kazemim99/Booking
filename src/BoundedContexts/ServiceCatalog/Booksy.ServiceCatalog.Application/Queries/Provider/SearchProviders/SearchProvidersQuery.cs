//===========================================
// Provider Query Definitions
//===========================================

//===========================================
// Queries/Provider/SearchProviders/SearchProvidersQuery.cs
//===========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.CQRS;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Application.DTOs.Provider;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.SearchProviders
{
    public sealed record SearchProvidersQuery(
        string? SearchTerm = null,
        ProviderType? Type = null,
        string? City = null,
        string? State = null,
        string? Country = null,
        bool? AllowsOnlineBooking = null,
        bool? OffersMobileServices = null,
        bool? VerifiedOnly = null,
        decimal? MinRating = null,
        bool IncludeInactive = false) : PaginatedQueryBase<ProviderSearchItem>();
}


// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Queries/Customer/GetCustomerFavoriteProviders/GetCustomerFavoriteProvidersQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.UserManagement.Application.CQRS.Queries.Customer.GetCustomerFavoriteProviders
{
    // Not cacheable: nothing evicted it, so a salon just added to (or removed from) the favourites did not show
    // (add-observability-and-caching).
    public sealed record GetCustomerFavoriteProvidersQuery(Guid CustomerId) : IQuery<List<FavoriteProviderViewModel>>;
}

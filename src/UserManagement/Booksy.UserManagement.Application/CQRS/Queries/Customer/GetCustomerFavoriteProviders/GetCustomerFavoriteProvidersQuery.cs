// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetCustomerFavoriteProviders/GetCustomerFavoriteProvidersQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerFavoriteProviders
{
    public sealed record GetCustomerFavoriteProvidersQuery(Guid CustomerId) : IQuery<List<FavoriteProviderViewModel>>
    {
        public bool IsCacheable => true;
        public string CacheKey => $"customer:favorites:{CustomerId}";
        public int CacheExpirationSeconds => 300; // 5 minutes
    }
}

// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetRecentlyVisitedProviders/GetRecentlyVisitedProvidersQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetRecentlyVisitedProviders
{
    /// <summary>
    /// Query to get customer's recently visited providers
    /// </summary>
    public sealed record GetRecentlyVisitedProvidersQuery(
        Guid CustomerId,
        int Limit = 20) : IQuery<List<RecentlyVisitedProviderViewModel>>;
}

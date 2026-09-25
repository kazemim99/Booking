// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Provider/GetProviderById/GetProviderByIdQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Application.Caching;
using AsanRezerve.ServiceCatalog.Application.Queries.Provider.SearchProviders;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetProviderById
{
    /// <summary>
    /// Query to get provider details by ID
    /// </summary>
    /// <remarks>
    /// Cached (the salon page is the customer apps' most-read screen): the same for every caller, and evicted by
    /// <see cref="ReadModelCacheTags.ForProviderChange"/> whenever the salon, its services or staff are saved.
    /// </remarks>
    public sealed record GetProviderByIdQuery(
        Guid ProviderId,
        bool IncludeServices = false,
        bool IncludeStaff = false) : IQuery<ProviderDetailsResult?>
    {
        public bool IsCacheable => true;

        public int? CacheExpirationSeconds => 300;

        public IReadOnlyCollection<string>? CacheTags => [ReadModelCacheTags.Provider(ProviderId)];
    }
}
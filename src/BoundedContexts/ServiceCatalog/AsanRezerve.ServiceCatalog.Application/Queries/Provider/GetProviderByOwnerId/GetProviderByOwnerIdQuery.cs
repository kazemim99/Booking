// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Provider/GetProviderByOwnerId/GetProviderByOwnerIdQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Application.Queries.Provider.SearchProviders;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetProviderByOwnerId
{
    /// <summary>
    /// Query to get provider details by owner ID
    /// </summary>
    public sealed record GetProviderByOwnerIdQuery(
        Guid OwnerUserId) : IQuery<ProviderDetailsResult?>;
}
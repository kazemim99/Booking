// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderByOwnerId/GetProviderByOwnerIdQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderByOwnerId
{
    /// <summary>
    /// Query to get provider details by owner ID
    /// </summary>
    public sealed record GetProviderByOwnerIdQuery(
        Guid OwnerUserId) : IQuery<ProviderDetailsViewModel?>;
}
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProvidersByStatus
{
    public sealed record GetProvidersByStatusQuery(
        ProviderStatus Status,
        int? MaxResults = null) : IQuery<IReadOnlyList<ProviderListViewModel>>;
}
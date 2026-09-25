using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetProvidersByStatus
{
    public sealed record GetProvidersByStatusQuery(
        ProviderStatus Status,
        int? MaxResults = null) : IQuery<IReadOnlyList<ProviderListViewModel>>;
}
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Service.GetPopularServices
{
    public sealed record GetPopularServicesQuery(
        string? CategoryFilter = null,
        int Limit = 20) : IQuery<IReadOnlyList<ServiceSummaryItem>>;
}


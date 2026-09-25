using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Service.GetServicesByStatus
{
    public sealed record GetServicesByStatusQuery(
        ServiceStatus Status,
        int MaxResults = 100) : IQuery<IReadOnlyList<ServiceSummaryItem>>;
}

using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Queries.Service.GetServicesByStatus
{
    public sealed record GetServicesByStatusQuery(
        ServiceStatus Status,
        int MaxResults = 100) : IQuery<IReadOnlyList<ServiceSummaryItem>>;
}

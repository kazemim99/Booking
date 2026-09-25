using AsanRezerve.Core.Application.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Service.GetServicesByProvider
{
    public sealed record GetServicesByProviderQuery(
        Guid ProviderId,
        ServiceStatus? Status = null,
        string? Category = null) : PaginatedQueryBase<ServiceSummaryItem>;
}

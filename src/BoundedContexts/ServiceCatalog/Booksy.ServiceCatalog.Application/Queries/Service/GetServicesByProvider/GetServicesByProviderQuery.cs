using Booksy.Core.Application.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Service.GetServicesByProvider
{
    public sealed record GetServicesByProviderQuery(
        Guid ProviderId,
        ServiceStatus? Status = null,
        string? Category = null) : PaginatedQueryBase<ServiceSummaryItem>;
}

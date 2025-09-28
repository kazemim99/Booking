
namespace Booksy.ServiceCatalog.Application.Queries.Service.GetServicesByProvider
{
    public sealed record ServicesByProviderViewModel(
        IReadOnlyList<ServiceSummaryItem> Services);
}


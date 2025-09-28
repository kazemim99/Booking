//===========================================
// Queries/Service/SearchServices/SearchServicesViewModel.cs
//===========================================
namespace Booksy.ServiceCatalog.Application.Queries.Service.SearchServices
{
    public sealed record SearchServicesViewModel(
        IReadOnlyList<ServiceSearchItem> Services,
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages,
        SearchFiltersApplied FiltersApplied);
}


//===========================================
// Queries/Service/SearchServices/SearchServicesViewModel.cs
//===========================================
namespace Booksy.ServiceCatalog.Application.Queries.Service.SearchServices
{
    public sealed record SearchFiltersApplied(
        string? SearchTerm,
        string? Category,
        string? Type,
        decimal? MinPrice,
        decimal? MaxPrice,
        int? MaxDurationMinutes,
        bool? AvailableAsMobile,
        string? Location);
}


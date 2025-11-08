// ========================================
// Booksy.ServiceCatalog.Application/Queries/Reports/GetCustomerHistory/CustomerHistoryDto.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Reports.GetCustomerHistory
{
    public sealed record CustomerHistoryDto(
        Guid CustomerId,
        int TotalBookings,
        int CompletedBookings,
        int CancelledBookings,
        decimal TotalSpent,
        string Currency,
        IReadOnlyList<FavoriteProviderDto> FavoriteProviders);

    public sealed record FavoriteProviderDto(
        Guid ProviderId,
        int BookingCount);
}

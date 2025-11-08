// ========================================
// RevenueStatisticsViewModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetProviderRevenue
{
    public sealed record RevenueStatisticsViewModel(
        Guid ProviderId,
        DateTime StartDate,
        DateTime EndDate,
        decimal TotalRevenue,
        decimal TotalRefunds,
        decimal NetRevenue,
        int SuccessfulPayments,
        int TotalPayments,
        decimal SuccessRate,
        string Currency);
}

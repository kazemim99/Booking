// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payment/GetProviderEarnings/ProviderEarningsViewModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetProviderEarnings
{
    public sealed record ProviderEarningsViewModel(
        Guid ProviderId,
        DateTime PeriodStart,
        DateTime EndDate,
        decimal GrossEarnings,
        decimal CommissionAmount,
        decimal NetEarnings,
        string Currency,
        int TotalPayments,
        int PaidPayments,
        int RefundedPayments,
        decimal TotalRefunded,
        List<EarningsByDateDto> EarningsByDate);

    public sealed record EarningsByDateDto(
        DateTime Date,
        decimal GrossAmount,
        decimal CommissionAmount,
        decimal NetAmount,
        int PaymentCount);
}

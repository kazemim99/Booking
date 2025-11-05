// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payout/GetPendingPayouts/PayoutSummaryDto.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Payout.GetPendingPayouts
{
    public sealed record PayoutSummaryDto(
        Guid PayoutId,
        Guid ProviderId,
        decimal GrossAmount,
        decimal CommissionAmount,
        decimal NetAmount,
        string Currency,
        DateTime PeriodStart,
        DateTime PeriodEnd,
        int PaymentCount,
        string Status,
        DateTime CreatedAt,
        DateTime? ScheduledAt);
}

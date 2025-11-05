// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payout/GetProviderPayouts/PayoutDetailsDto.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Payout.GetProviderPayouts
{
    public sealed record PayoutDetailsDto(
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
        string? ExternalPayoutId,
        string? BankAccountLast4,
        string? BankName,
        DateTime CreatedAt,
        DateTime? ScheduledAt,
        DateTime? PaidAt,
        DateTime? FailedAt,
        string? FailureReason);
}

// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Payout/GetProviderPayouts/PayoutDetailsDto.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Queries.Payout.GetProviderPayouts
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
        string? FailureReason)
    {
        /// <summary>
        /// Projects a payout aggregate. One definition, so the by-id and by-provider reads cannot
        /// describe the same payout differently.
        /// </summary>
        public static PayoutDetailsDto From(Domain.Aggregates.PayoutAggregate.Payout payout) =>
            new(payout.Id.Value,
                payout.ProviderId.Value,
                payout.GrossAmount.Amount,
                payout.CommissionAmount.Amount,
                payout.NetAmount.Amount,
                payout.NetAmount.Currency,
                payout.PeriodStart,
                payout.PeriodEnd,
                payout.PaymentIds.Count,
                payout.Status.ToString(),
                payout.ExternalPayoutId,
                payout.BankAccountLast4,
                payout.BankName,
                payout.CreatedAt,
                payout.ScheduledAt,
                payout.PaidAt,
                payout.FailedAt,
                payout.FailureReason);
    }
}

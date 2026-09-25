// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Payout/CreatePayout/CreatePayoutResult.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Commands.Payout.CreatePayout
{
    public sealed record CreatePayoutResult(
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
        DateTime CreatedAt);
}

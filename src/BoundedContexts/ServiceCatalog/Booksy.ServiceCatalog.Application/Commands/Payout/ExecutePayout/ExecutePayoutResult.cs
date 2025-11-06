// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payout/ExecutePayout/ExecutePayoutResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Payout.ExecutePayout
{
    public sealed record ExecutePayoutResult(
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
        string ExternalPayoutId,
        DateTime? ArrivalDate,
        bool IsSuccessful,
        string? ErrorMessage,
        DateTime CreatedAt);
}

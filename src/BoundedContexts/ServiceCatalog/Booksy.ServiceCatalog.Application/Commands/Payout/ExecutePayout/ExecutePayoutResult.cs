// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payout/ExecutePayout/ExecutePayoutResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Payout.ExecutePayout
{
    public sealed record ExecutePayoutResult(
        Guid PayoutId,
        Guid ProviderId,
        decimal NetAmount,
        string Currency,
        string Status,
        string ExternalPayoutId,
        DateTime? ArrivalDate);
}

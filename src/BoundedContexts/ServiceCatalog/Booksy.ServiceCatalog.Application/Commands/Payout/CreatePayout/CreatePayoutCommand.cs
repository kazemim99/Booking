// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payout/CreatePayout/CreatePayoutCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions;

namespace Booksy.ServiceCatalog.Application.Commands.Payout.CreatePayout
{
    /// <summary>
    /// Command to create a payout for a provider based on payments in a period
    /// </summary>
    public sealed record CreatePayoutCommand(
        Guid ProviderId,
        DateTime PeriodStart,
        DateTime PeriodEnd,
        decimal? CommissionPercentage = null,
        DateTime? ScheduledAt = null,
        string? Notes = null,
        Guid? IdempotencyKey = null) : ICommand<CreatePayoutResult>;
}

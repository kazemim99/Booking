// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payout/ExecutePayout/ExecutePayoutCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions;

namespace Booksy.ServiceCatalog.Application.Commands.Payout.ExecutePayout
{
    /// <summary>
    /// Command to execute a pending payout via payment gateway
    /// </summary>
    public sealed record ExecutePayoutCommand(
        Guid PayoutId,
        string? ConnectedAccountId = null,
        Guid? IdempotencyKey = null) : ICommand<ExecutePayoutResult>;
}

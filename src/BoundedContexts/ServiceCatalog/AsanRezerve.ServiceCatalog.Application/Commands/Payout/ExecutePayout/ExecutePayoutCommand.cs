// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Payout/ExecutePayout/ExecutePayoutCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Payout.ExecutePayout
{
    /// <summary>
    /// Command to execute a pending payout via payment gateway
    /// </summary>
    /// <remarks>
    /// Implements <see cref="Core.Application.Abstractions.CQRS.INonTransactionalCommand"/> because the
    /// handler calls <c>IPaymentGateway.CreatePayoutAsync</c> — real money leaving the platform. Inside
    /// the ambient retried transaction, a transient DB fault would re-run the handler and pay the
    /// provider twice. The handler was changed alongside this marker to commit its own single unit
    /// (it previously relied on <c>TransactionBehavior</c> to save, which opting out removes).
    /// Enforced by <c>MoneyCommandTransactionTests</c>.
    /// </remarks>
    public sealed record ExecutePayoutCommand(
        Guid PayoutId,
        string? ConnectedAccountId = null,
        Guid? IdempotencyKey = null)
        : ICommand<ExecutePayoutResult>,
          Core.Application.Abstractions.CQRS.INonTransactionalCommand;
}

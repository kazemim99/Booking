namespace Booksy.Core.Application.Abstractions.CQRS
{
    /// <summary>
    /// Opts a command out of the ambient <c>TransactionBehavior</c> transaction.
    /// <para>
    /// The default <c>TransactionBehavior</c> runs every command inside
    /// <c>IUnitOfWork.ExecuteInTransactionAsync</c>, which executes on a <b>retrying</b> Npgsql execution
    /// strategy (<c>EnableRetryOnFailure</c>). If a command calls an external payment gateway inside that scope,
    /// a transient DB fault would re-run the whole handler — <b>including the gateway call</b> — double-charging
    /// or double-refunding the customer.
    /// </para>
    /// <para>
    /// Money-moving commands implement this marker so the gateway call happens exactly once, outside any retried
    /// transaction. Such handlers MUST persist their own work via a single <c>CommitAsync</c> (one
    /// <c>SaveChanges</c> is itself a retry-safe unit). If the commit fails after the gateway succeeded, the
    /// payment-reconciliation sweep converges the record from the gateway's truth (money is never lost).
    /// </para>
    /// </summary>
    public interface INonTransactionalCommand
    {
    }
}

namespace Booksy.ServiceCatalog.Domain.Aggregates.LedgerAggregate
{
    /// <summary>
    /// The minimal chart of accounts for the platform's double-entry ledger. Every money movement is recorded as
    /// balanced entries across these accounts so balances and platform economics are auditable and reconstructable.
    /// </summary>
    public enum LedgerAccount
    {
        /// <summary>Cash held at / cleared through the payment gateway (an asset from the platform's view).</summary>
        GatewayClearing = 0,

        /// <summary>Amounts owed to providers for completed bookings (a liability until paid out).</summary>
        ProviderPayable = 1,

        /// <summary>The platform's commission / fee income (revenue).</summary>
        PlatformRevenue = 2,

        /// <summary>The customer's side of a charge/refund (counterparty of gateway movements).</summary>
        Customer = 3
    }

    /// <summary>Direction of a single ledger entry. Debit increases assets/expenses; Credit increases liabilities/revenue.</summary>
    public enum LedgerDirection
    {
        Debit = 0,
        Credit = 1
    }

    /// <summary>What money event produced a ledger entry — for audit and reconciliation grouping.</summary>
    public enum LedgerEntryType
    {
        Charge = 0,
        Refund = 1,
        Commission = 2,
        Payout = 3
    }
}

using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.LedgerAggregate;
using Xunit;

namespace Booksy.ServiceCatalog.Domain.UnitTests.LedgerAggregate;

/// <summary>
/// C5 financial-ledger: every money movement must produce balanced, immutable, double-entry records. These tests
/// lock in the core invariant — the signed amounts of a ledger transaction always sum to exactly zero — and the
/// account mapping for charge, refund and payout, including randomized property checks for mathematical confidence.
/// </summary>
public class LedgerTransactionTests
{
    private static Money Usd(decimal amount) => Money.Create(amount, "USD");

    private static LedgerEntry Entry(LedgerTransaction tx, LedgerAccount account) =>
        tx.Entries.Single(e => e.Account == account);

    // ---------------------------------------------------------------- Charge

    [Fact]
    public void ForCharge_produces_balanced_double_entry_with_correct_accounts()
    {
        var eventId = Guid.NewGuid();
        var tx = LedgerTransaction.ForCharge(eventId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Usd(100m), Usd(15m));

        Assert.Equal(0m, tx.Entries.Sum(e => e.SignedAmount)); // double-entry must balance
        Assert.Equal(3, tx.Entries.Count);

        var gateway = Entry(tx, LedgerAccount.GatewayClearing);
        Assert.Equal(LedgerDirection.Debit, gateway.Direction);
        Assert.Equal(100m, gateway.Amount.Amount);

        var revenue = Entry(tx, LedgerAccount.PlatformRevenue);
        Assert.Equal(LedgerDirection.Credit, revenue.Direction);
        Assert.Equal(15m, revenue.Amount.Amount);

        var payable = Entry(tx, LedgerAccount.ProviderPayable);
        Assert.Equal(LedgerDirection.Credit, payable.Direction);
        Assert.Equal(85m, payable.Amount.Amount);
    }

    [Fact]
    public void ForCharge_with_commission_exceeding_gross_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            LedgerTransaction.ForCharge(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Usd(50m), Usd(60m)));
        Assert.Contains("Commission cannot exceed", ex.Message);
    }

    // ---------------------------------------------------------------- Refund

    [Fact]
    public void ForRefund_produces_balanced_reversal()
    {
        var tx = LedgerTransaction.ForRefund(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Usd(40m), Usd(6m));

        Assert.Equal(0m, tx.Entries.Sum(e => e.SignedAmount));

        var gateway = Entry(tx, LedgerAccount.GatewayClearing);
        Assert.Equal(LedgerDirection.Credit, gateway.Direction);
        Assert.Equal(40m, gateway.Amount.Amount);

        var revenue = Entry(tx, LedgerAccount.PlatformRevenue);
        Assert.Equal(LedgerDirection.Debit, revenue.Direction);
        Assert.Equal(6m, revenue.Amount.Amount);

        var payable = Entry(tx, LedgerAccount.ProviderPayable);
        Assert.Equal(LedgerDirection.Debit, payable.Direction);
        Assert.Equal(34m, payable.Amount.Amount);
    }

    // ---------------------------------------------------------------- Payout

    [Fact]
    public void ForPayout_reduces_provider_payable_and_gateway_by_equal_amounts()
    {
        var providerId = Guid.NewGuid();
        var tx = LedgerTransaction.ForPayout(Guid.NewGuid(), providerId, Usd(85m));

        Assert.Equal(0m, tx.Entries.Sum(e => e.SignedAmount));

        var payable = Entry(tx, LedgerAccount.ProviderPayable);
        Assert.Equal(LedgerDirection.Debit, payable.Direction);
        Assert.Equal(85m, payable.Amount.Amount);

        var gateway = Entry(tx, LedgerAccount.GatewayClearing);
        Assert.Equal(LedgerDirection.Credit, gateway.Direction);
        Assert.Equal(85m, gateway.Amount.Amount);

        Assert.All(tx.Entries, e => Assert.Equal(providerId, e.ProviderId));
    }

    [Fact]
    public void ForPayout_with_commission_recognizes_platform_revenue_and_pays_net()
    {
        // Gross owed 100, commission 15 → provider paid net 85, platform books 15 revenue.
        var tx = LedgerTransaction.ForPayout(Guid.NewGuid(), Guid.NewGuid(), Usd(100m), Usd(15m));

        Assert.Equal(0m, tx.Entries.Sum(e => e.SignedAmount));
        Assert.Equal(3, tx.Entries.Count);

        var payable = Entry(tx, LedgerAccount.ProviderPayable);
        Assert.Equal(LedgerDirection.Debit, payable.Direction);
        Assert.Equal(100m, payable.Amount.Amount);

        var revenue = Entry(tx, LedgerAccount.PlatformRevenue);
        Assert.Equal(LedgerDirection.Credit, revenue.Direction);
        Assert.Equal(15m, revenue.Amount.Amount);

        var gateway = Entry(tx, LedgerAccount.GatewayClearing);
        Assert.Equal(LedgerDirection.Credit, gateway.Direction);
        Assert.Equal(85m, gateway.Amount.Amount); // net paid out
    }

    // ---------------------------------------------------------------- Constructor safety net

    [Fact]
    public void Constructor_rejects_an_unbalanced_set()
    {
        var eventId = Guid.NewGuid();
        var unbalanced = new List<LedgerEntry>
        {
            new(eventId, LedgerAccount.GatewayClearing, LedgerDirection.Debit, LedgerEntryType.Charge, Usd(100m), null, null, null),
            new(eventId, LedgerAccount.ProviderPayable, LedgerDirection.Credit, LedgerEntryType.Charge, Usd(90m), null, null, null),
        };

        var ex = Assert.Throws<InvalidOperationException>(() => new LedgerTransaction(eventId, unbalanced));
        Assert.Contains("not balanced", ex.Message);
    }

    [Fact]
    public void Constructor_rejects_mixed_currencies()
    {
        var eventId = Guid.NewGuid();
        var mixed = new List<LedgerEntry>
        {
            new(eventId, LedgerAccount.GatewayClearing, LedgerDirection.Debit, LedgerEntryType.Charge, Money.Create(100m, "USD"), null, null, null),
            new(eventId, LedgerAccount.ProviderPayable, LedgerDirection.Credit, LedgerEntryType.Charge, Money.Create(100m, "EUR"), null, null, null),
        };

        var ex = Assert.Throws<InvalidOperationException>(() => new LedgerTransaction(eventId, mixed));
        Assert.Contains("single-currency", ex.Message);
    }

    // ---------------------------------------------------------------- Property-based (randomized)

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Charge_and_refund_always_balance_for_random_amounts(int seed)
    {
        var rng = new Random(seed);
        for (var i = 0; i < 500; i++)
        {
            var gross = Math.Round((decimal)(rng.NextDouble() * 10_000), 2);
            if (gross <= 0) continue;
            var commission = Math.Round((decimal)rng.NextDouble() * gross, 2); // 0..gross

            var charge = LedgerTransaction.ForCharge(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Usd(gross), Usd(commission));
            Assert.Equal(0m, charge.Entries.Sum(e => e.SignedAmount));
            Assert.Equal(gross - commission, Entry(charge, LedgerAccount.ProviderPayable).Amount.Amount);

            var refund = Math.Round((decimal)rng.NextDouble() * gross, 2);
            var commissionReversed = Math.Round((decimal)rng.NextDouble() * refund, 2);
            var refundTx = LedgerTransaction.ForRefund(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Usd(refund), Usd(commissionReversed));
            Assert.Equal(0m, refundTx.Entries.Sum(e => e.SignedAmount));
        }
    }

    [Fact]
    public void Account_balances_aggregate_correctly_across_a_charge_then_full_payout()
    {
        // Charge 100 (commission 15) then pay the provider their 85; provider-payable nets to zero, platform keeps 15.
        var all = new List<LedgerEntry>();
        all.AddRange(LedgerTransaction.ForCharge(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Usd(100m), Usd(15m)).Entries);
        all.AddRange(LedgerTransaction.ForPayout(Guid.NewGuid(), Guid.NewGuid(), Usd(85m)).Entries);

        decimal Balance(LedgerAccount a) => all.Where(e => e.Account == a).Sum(e => e.SignedAmount);

        Assert.Equal(0m, Balance(LedgerAccount.ProviderPayable)); // provider fully paid out
        Assert.Equal(-15m, Balance(LedgerAccount.PlatformRevenue)); // credit balance of 15
        Assert.Equal(15m, Balance(LedgerAccount.GatewayClearing)); // 100 in - 85 out
        Assert.Equal(0m, all.Sum(e => e.SignedAmount)); // whole ledger always balances
    }
}

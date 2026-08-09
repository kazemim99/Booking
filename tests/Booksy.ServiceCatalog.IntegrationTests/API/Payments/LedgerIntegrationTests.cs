using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.LedgerAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PayoutAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Payments;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Payments;

/// <summary>
/// C5 financial-ledger. Proves the ledger is an append-only, immutable audit log with idempotent, replay- and
/// duplicate-safe posting, and that reconciliation closes any gap left by a partial failure — so every monetary
/// movement is traceable from payment to ledger without gaps.
/// </summary>
public class LedgerIntegrationTests : Infrastructure.ServiceCatalogIntegrationTestBase
{
    public LedgerIntegrationTests(Infrastructure.ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory) { }

    /// Seeds a Paid payment through the production path; committing dispatches PaymentVerifiedEvent, which the
    /// ledger charge handler posts atomically with the payment. Returns the payment id.
    private async Task<Guid> SeedPaidPaymentAsync(decimal amount = 100m, Guid? providerId = null)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();

        var p = Payment.CreateForBooking(
            BookingId.From(Guid.NewGuid()), UserId.From(Guid.NewGuid()), ProviderId.From(providerId ?? Guid.NewGuid()),
            Money.Create(amount, "USD"), PaymentMethod.ZarinPal);
        p.RecordPaymentRequest($"auth-{Guid.NewGuid():N}", "https://pay.test/authority");
        p.VerifyPayment("REF-1", "6274********1234", 0); // -> Paid, raises PaymentVerifiedEvent
        await repo.AddAsync(p);
        await uow.CommitAsync();
        return p.Id.Value;
    }

    [Fact]
    public async Task Charge_is_posted_to_the_ledger_atomically_when_a_payment_is_verified()
    {
        var paymentId = await SeedPaidPaymentAsync(100m);

        using var scope = Factory.Services.CreateScope();
        var ledger = scope.ServiceProvider.GetRequiredService<ILedgerRepository>();
        var entries = await ledger.GetByPaymentAsync(paymentId);

        entries.Should().HaveCount(2, "a charge posts a Gateway debit and a ProviderPayable credit");
        entries.Sum(e => e.SignedAmount).Should().Be(0m, "the ledger always balances");
        entries.Single(e => e.Account == LedgerAccount.GatewayClearing)
            .Should().Match<LedgerEntry>(e => e.Direction == LedgerDirection.Debit && e.Amount.Amount == 100m);
        entries.Single(e => e.Account == LedgerAccount.ProviderPayable)
            .Should().Match<LedgerEntry>(e => e.Direction == LedgerDirection.Credit && e.Amount.Amount == 100m);
    }

    [Fact]
    public async Task Posting_is_idempotent_across_replays_and_duplicate_events()
    {
        var paymentId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var eventId = LedgerEventKeys.Charge(paymentId); // deterministic → same key on every replay

        async Task<bool> PostOnceAsync()
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var ledger = scope.ServiceProvider.GetRequiredService<ILedgerRepository>();
            var tx = LedgerTransaction.ForCharge(eventId, Guid.NewGuid(), paymentId, providerId, Money.Create(50m, "USD"));
            var posted = await ledger.AppendAsync(tx);
            await db.SaveChangesAsync();
            return posted;
        }

        (await PostOnceAsync()).Should().BeTrue("first delivery posts");
        (await PostOnceAsync()).Should().BeFalse("a replayed/duplicate event is a no-op");
        (await PostOnceAsync()).Should().BeFalse();

        using var verify = Factory.Services.CreateScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var count = await verifyDb.LedgerEntries.CountAsync(e => e.EventId == eventId);
        count.Should().Be(2, "exactly one balanced charge set, never duplicated");
    }

    [Fact]
    public async Task The_unique_index_rejects_a_forced_duplicate_bypassing_the_idempotency_check()
    {
        var eventId = LedgerEventKeys.Charge(Guid.NewGuid());
        var providerId = Guid.NewGuid();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var tx = LedgerTransaction.ForCharge(eventId, null, Guid.NewGuid(), providerId, Money.Create(10m, "USD"));
            await db.LedgerEntries.AddRangeAsync(tx.Entries);
            await db.SaveChangesAsync();
        }

        // Force a second post of the SAME (EventId, Account) directly, bypassing AppendAsync's check.
        using var scope2 = Factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var dup = LedgerTransaction.ForCharge(eventId, null, Guid.NewGuid(), providerId, Money.Create(10m, "USD"));
        await db2.LedgerEntries.AddRangeAsync(dup.Entries);

        var act = async () => await db2.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>("the unique (EventId, Account) index is the hard backstop");
    }

    [Fact]
    public async Task Ledger_entries_are_immutable_updates_and_deletes_are_rejected()
    {
        var eventId = LedgerEventKeys.Charge(Guid.NewGuid());
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var tx = LedgerTransaction.ForCharge(eventId, null, Guid.NewGuid(), Guid.NewGuid(), Money.Create(25m, "USD"));
            await db.LedgerEntries.AddRangeAsync(tx.Entries);
            await db.SaveChangesAsync();
        }

        // UPDATE is rejected.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var entry = await db.LedgerEntries.FirstAsync(e => e.EventId == eventId);
            db.Entry(entry).State = EntityState.Modified;
            var act = async () => await db.SaveChangesAsync();
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*append-only*");
        }

        // DELETE is rejected.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var entry = await db.LedgerEntries.FirstAsync(e => e.EventId == eventId);
            db.LedgerEntries.Remove(entry);
            var act = async () => await db.SaveChangesAsync();
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*append-only*");
        }
    }

    [Fact]
    public async Task Reconciliation_reposts_a_missing_charge_entry_after_a_partial_failure_and_is_idempotent()
    {
        var paymentId = await SeedPaidPaymentAsync(70m);

        // Simulate a partial failure: the payment is Paid but its ledger entries were lost. Raw SQL DELETE bypasses
        // the append-only guard (which only governs EF SaveChanges) to reproduce the gap.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""ServiceCatalog"".""LedgerEntries"" WHERE ""PaymentId"" = {0}", paymentId);
        }

        int firstRun, secondRun;
        using (var scope = Factory.Services.CreateScope())
            firstRun = await scope.ServiceProvider.GetRequiredService<ILedgerReconciler>().ReconcileMissingChargeEntriesAsync();
        using (var scope = Factory.Services.CreateScope())
            secondRun = await scope.ServiceProvider.GetRequiredService<ILedgerReconciler>().ReconcileMissingChargeEntriesAsync();

        firstRun.Should().BeGreaterThanOrEqualTo(1, "the missing charge is recovered");
        secondRun.Should().Be(0, "reconciliation is idempotent — the gap is already closed");

        using var verify = Factory.Services.CreateScope();
        var ledger = verify.ServiceProvider.GetRequiredService<ILedgerRepository>();
        var entries = await ledger.GetByPaymentAsync(paymentId);
        entries.Should().NotBeEmpty("the charge is traceable from the payment again");
        entries.Sum(e => e.SignedAmount).Should().Be(0m);
    }

    [Fact]
    public async Task End_to_end_charge_then_payout_leaves_a_fully_balanced_traceable_ledger()
    {
        // Every monetary movement is traceable and the whole ledger balances: a customer charge credits the
        // provider-payable; a completed payout clears it, recognizes commission as revenue, and pays the net.
        var providerId = Guid.NewGuid();
        await SeedPaidPaymentAsync(100m, providerId); // charge 100 → ProviderPayable credit 100, GatewayClearing debit 100

        using (var scope = Factory.Services.CreateScope())
        {
            var payoutRepo = scope.ServiceProvider.GetRequiredService<IPayoutWriteRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();

            var payout = Payout.Create(
                ProviderId.From(providerId), Money.Create(100m, "USD"), Money.Create(15m, "USD"),
                DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, Array.Empty<PaymentId>());
            payout.Schedule(DateTime.UtcNow.AddMinutes(1));
            payout.MarkAsProcessing("ext-payout-1");
            payout.MarkAsPaid("1234", "Test Bank"); // -> PayoutCompletedEvent → ledger payout posting

            await payoutRepo.AddAsync(payout);
            await uow.CommitAsync();
        }

        using var verify = Factory.Services.CreateScope();
        var ledger = verify.ServiceProvider.GetRequiredService<ILedgerRepository>();

        // ProviderPayable: +100 credit (charge) then -100 debit (payout) = 0.
        (await ledger.GetAccountBalanceAsync(LedgerAccount.ProviderPayable, providerId)).Should().Be(0m);
        // PlatformRevenue: 15 commission recognized at payout (a credit balance).
        (await ledger.GetAccountBalanceAsync(LedgerAccount.PlatformRevenue, providerId)).Should().Be(-15m);
        // GatewayClearing: 100 in − 85 net out = 15 retained (= commission).
        (await ledger.GetAccountBalanceAsync(LedgerAccount.GatewayClearing, providerId)).Should().Be(15m);
    }

    /// Drives a payout aggregate to Paid (raising PayoutCompletedEvent → ledger payout posting).
    private async Task CompletePayoutAsync(Guid providerId, decimal gross, decimal commission)
    {
        using var scope = Factory.Services.CreateScope();
        var payoutRepo = scope.ServiceProvider.GetRequiredService<IPayoutWriteRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
        var payout = Payout.Create(
            ProviderId.From(providerId), Money.Create(gross, "USD"), Money.Create(commission, "USD"),
            DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, Array.Empty<PaymentId>());
        payout.Schedule(DateTime.UtcNow.AddMinutes(1));
        payout.MarkAsProcessing("ext-payout");
        payout.MarkAsPaid("1234", "Test Bank");
        await payoutRepo.AddAsync(payout);
        await uow.CommitAsync();
    }

    [Fact]
    public async Task Refund_after_payout_records_a_negative_balance_and_blocks_further_payouts_with_no_money_lost()
    {
        // charge 100 → payout (gross 100, commission 15, net 85) → refund 30.
        var providerId = Guid.NewGuid();
        var paymentId = await SeedPaidPaymentAsync(100m, providerId);
        await CompletePayoutAsync(providerId, gross: 100m, commission: 15m);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var ledger = scope.ServiceProvider.GetRequiredService<ILedgerRepository>();
            await ledger.AppendAsync(LedgerTransaction.ForRefund(
                LedgerEventKeys.Refund(paymentId, DateTime.UtcNow), null, paymentId, providerId, Money.Create(30m, "USD")));
            await db.SaveChangesAsync();
        }

        using var verify = Factory.Services.CreateScope();
        var ledgerRepo = verify.ServiceProvider.GetRequiredService<ILedgerRepository>();

        // No money is lost or untraceable: this provider's movements across all accounts sum to zero (every entry
        // carries the provider id, so the provider-scoped ledger balances — isolated from other tests' data).
        var gw = await ledgerRepo.GetAccountBalanceAsync(LedgerAccount.GatewayClearing, providerId);
        var pp = await ledgerRepo.GetAccountBalanceAsync(LedgerAccount.ProviderPayable, providerId);
        var pr = await ledgerRepo.GetAccountBalanceAsync(LedgerAccount.PlatformRevenue, providerId);
        (gw + pp + pr).Should().Be(0m, "the provider's ledger balances even after a refund-after-payout — no money lost");
        // The provider was overpaid by the refunded amount → negative payable → future payouts blocked (recovered later).
        (await ledgerRepo.GetProviderPayableBalanceAsync(providerId)).Should().Be(-30m);

        var handler = new Application.Commands.Payout.CreatePayout.CreatePayoutCommandHandler(
            ledgerRepo, verify.ServiceProvider.GetRequiredService<IPayoutWriteRepository>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Application.Commands.Payout.CreatePayout.CreatePayoutCommandHandler>.Instance);
        var act = async () => await handler.Handle(new Application.Commands.Payout.CreatePayout.CreatePayoutCommand(
            providerId, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>("a provider with a negative balance cannot be paid again until it recovers");
    }

    [Fact]
    public async Task Payout_amount_is_derived_from_the_ledger_balance_and_refunds_reduce_it()
    {
        var providerId = Guid.NewGuid();
        var paymentId = await SeedPaidPaymentAsync(100m, providerId); // ledger owes provider 100

        // A refund of 40 debits ProviderPayable → owed drops to 60 (refund-to-payout handling).
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var ledger = scope.ServiceProvider.GetRequiredService<ILedgerRepository>();
            await ledger.AppendAsync(LedgerTransaction.ForRefund(
                LedgerEventKeys.Refund(paymentId, DateTime.UtcNow), null, paymentId, providerId, Money.Create(40m, "USD")));
            await db.SaveChangesAsync();
        }

        using var handlerScope = Factory.Services.CreateScope();
        var handler = new Application.Commands.Payout.CreatePayout.CreatePayoutCommandHandler(
            handlerScope.ServiceProvider.GetRequiredService<ILedgerRepository>(),
            handlerScope.ServiceProvider.GetRequiredService<IPayoutWriteRepository>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Application.Commands.Payout.CreatePayout.CreatePayoutCommandHandler>.Instance);

        var result = await handler.Handle(new Application.Commands.Payout.CreatePayout.CreatePayoutCommand(
            providerId, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, CommissionPercentage: 15m), CancellationToken.None);

        result.GrossAmount.Should().Be(60m, "payout gross = ledger provider-payable balance (100 − 40 refund)");
        result.CommissionAmount.Should().Be(9m, "15% of 60");
        result.NetAmount.Should().Be(51m);
    }

    [Fact]
    public async Task Payout_is_blocked_when_the_ledger_balance_is_not_positive()
    {
        // Provider fully refunded → ledger owes nothing → payout blocked (block-future-payouts default).
        var providerId = Guid.NewGuid();
        var paymentId = await SeedPaidPaymentAsync(50m, providerId);
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var ledger = scope.ServiceProvider.GetRequiredService<ILedgerRepository>();
            await ledger.AppendAsync(LedgerTransaction.ForRefund(
                LedgerEventKeys.Refund(paymentId, DateTime.UtcNow), null, paymentId, providerId, Money.Create(50m, "USD")));
            await db.SaveChangesAsync();
        }

        using var handlerScope = Factory.Services.CreateScope();
        var handler = new Application.Commands.Payout.CreatePayout.CreatePayoutCommandHandler(
            handlerScope.ServiceProvider.GetRequiredService<ILedgerRepository>(),
            handlerScope.ServiceProvider.GetRequiredService<IPayoutWriteRepository>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Application.Commands.Payout.CreatePayout.CreatePayoutCommandHandler>.Instance);

        var act = async () => await handler.Handle(new Application.Commands.Payout.CreatePayout.CreatePayoutCommand(
            providerId, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no payable ledger balance*");
    }

    [Fact]
    public async Task Balance_drift_detection_is_zero_for_a_healthy_ledger_and_nonzero_when_corrupted()
    {
        await SeedPaidPaymentAsync(100m); // balanced postings

        using (var scope = Factory.Services.CreateScope())
        {
            var reconciler = scope.ServiceProvider.GetRequiredService<ILedgerReconciler>();
            (await reconciler.DetectBalanceDriftAsync()).Should().Be(0m, "a correct double-entry ledger always balances");
        }

        // Corrupt the ledger with a single unbalanced debit (raw SQL bypasses the balanced-transaction factory).
        var corruptId = Guid.NewGuid();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO ""ServiceCatalog"".""LedgerEntries""
                  (""Id"",""EventId"",""Account"",""Direction"",""EntryType"",""Amount"",""Currency"",""PostedAt"",""IsDeleted"",""CreatedAt"")
                  VALUES ({0},{1},'GatewayClearing','Debit','Charge',999,'USD', now(), false, now())",
                corruptId, Guid.NewGuid());
        }

        try
        {
            using var scope = Factory.Services.CreateScope();
            var reconciler = scope.ServiceProvider.GetRequiredService<ILedgerReconciler>();
            (await reconciler.DetectBalanceDriftAsync()).Should().Be(999m, "the injected unbalanced entry is detected as drift");
        }
        finally
        {
            // Clean up the injected corruption (raw SQL bypasses the append-only guard) so it does not pollute the
            // whole-ledger drift check used by other tests / the background maintenance service.
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""ServiceCatalog"".""LedgerEntries"" WHERE ""Id"" = {0}", corruptId);
        }
    }

    [Fact]
    public async Task Provider_payable_balance_reflects_a_charge()
    {
        var providerId = Guid.NewGuid();
        var eventId = LedgerEventKeys.Charge(Guid.NewGuid());
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var ledger = scope.ServiceProvider.GetRequiredService<ILedgerRepository>();
            await ledger.AppendAsync(LedgerTransaction.ForCharge(eventId, null, Guid.NewGuid(), providerId, Money.Create(90m, "USD")));
            await db.SaveChangesAsync();
        }

        using var verify = Factory.Services.CreateScope();
        var repo = verify.ServiceProvider.GetRequiredService<ILedgerRepository>();
        // ProviderPayable is a credit balance → negative signed sum.
        (await repo.GetAccountBalanceAsync(LedgerAccount.ProviderPayable, providerId)).Should().Be(-90m);
    }
}

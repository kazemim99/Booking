using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.External.Payment.ZarinPal;
using Booksy.Infrastructure.External.Payment.ZarinPal.Models;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Payments;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Payments;

/// <summary>
/// C2, invariants I1/I3 — money is never lost, duplicated, or untraceable. Proves the reconciliation sweep
/// both <b>detects</b> stale Pending payments (lost callback / crash between charge and confirm) and
/// <b>converges</b> them to their true gateway state (Paid / Failed), treating the gateway strictly as a
/// read-only oracle and the database as the record of truth. Covers the fault matrix: gateway timeout/throw
/// (stays Pending, recoverable next sweep), already-settled (no-op / idempotent), and verified/not-verified
/// convergence with a recorded transaction (never untraceable).
///
/// The gateway is a programmable fake so no real ZarinPal call is made; the reconciler is constructed with the
/// real DbContext / repository / unit-of-work from the test scope.
/// </summary>
public class PaymentReconciliationTests : ServiceCatalogIntegrationTestBase
{
    public PaymentReconciliationTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    // --- programmable ZarinPal fake (read-only oracle) ---------------------------------------------------
    private sealed class FakeZarinPal : IZarinPalService
    {
        public Func<string, decimal, ZarinPalVerifyResult> OnVerify = (_, _) =>
            new ZarinPalVerifyResult { IsSuccessful = false, ErrorCode = -1, ErrorMessage = "unset" };
        public int VerifyCalls;

        public Task<ZarinPalVerifyResult> VerifyPaymentAsync(string authority, decimal amount, CancellationToken ct = default)
        {
            VerifyCalls++;
            return Task.FromResult(OnVerify(authority, amount)); // OnVerify may throw to simulate timeout/network failure
        }

        public Task<ZarinPalPaymentResult> CreatePaymentRequestAsync(
            decimal amount, string description, string? mobile = null, string? email = null, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<ZarinPalRefundResult> RefundPaymentAsync(
            string authority, decimal amount, string? description = null, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private static PaymentReconciler NewReconciler(IServiceScope scope, IZarinPalService gateway) =>
        new(
            scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>(),
            gateway,
            scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>(),
            scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>(),
            NullLogger<PaymentReconciler>.Instance);

    /// Seeds a stale Pending payment via the production creation path, then backdates CreatedAt so the sweep
    /// treats it as stale. Returns (id, authority).
    private async Task<(Guid id, string authority)> SeedStalePendingAsync()
    {
        var authority = $"auth-{Guid.NewGuid():N}";
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();

        var payment = Payment.CreateForBooking(
            BookingId.From(Guid.NewGuid()), UserId.From(Guid.NewGuid()), ProviderId.From(Guid.NewGuid()),
            Money.Create(100, "USD"), PaymentMethod.ZarinPal);
        payment.RecordPaymentRequest(authority, "https://pay.test/authority"); // sets Authority, stays Pending
        await repo.AddAsync(payment);
        await uow.CommitAsync();

        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            @"UPDATE ""ServiceCatalog"".""Payments"" SET ""CreatedAt"" = now() - interval '1 hour' WHERE ""PaymentId"" = {0}",
            payment.Id.Value);
        return (payment.Id.Value, authority);
    }

    private async Task<Payment?> LoadAsync(string authority)
    {
        using var scope = Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>().GetByAuthorityAsync(authority);
    }

    [Fact]
    public async Task Sweep_detects_stale_pending_payments_with_authority_for_gateway_requery()
    {
        var (_, authority) = await SeedStalePendingAsync();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();

        var candidates = await db.Database.SqlQueryRaw<string>(
            @"SELECT ""Authority"" AS ""Value"" FROM ""ServiceCatalog"".""Payments""
              WHERE ""Status"" = 'Pending' AND ""Authority"" IS NOT NULL
                AND ""CreatedAt"" < now() - make_interval(mins => {0})
              ORDER BY ""CreatedAt"" LIMIT {1}", 0, 100)
            .ToListAsync();

        candidates.Should().Contain(authority,
            "a charged-but-unconfirmed payment must be detected so it is re-queried against the gateway and never lost");
    }

    // The shared test DB has no per-test cleanup and classes may run in parallel, so the global sweep can pick up
    // other tests' stale Pending payments. Each fake is therefore scoped to THIS test's authority — it acts only
    // on our payment and throws for any other, leaving other tests' payments untouched (Pending). Assertions are
    // per-authority, never on the global reconciled count.
    private static Func<string, decimal, ZarinPalVerifyResult> OnlyFor(string authority, ZarinPalVerifyResult result)
        => (auth, _) => auth == authority ? result : throw new TaskCanceledException("not this test's payment");

    [Fact]
    public async Task Converges_stale_pending_to_Paid_when_gateway_verifies_and_records_a_transaction()
    {
        // I1 (never lost): a charge whose callback was lost is settled to Paid from the gateway's truth.
        var (_, authority) = await SeedStalePendingAsync();
        var gateway = new FakeZarinPal
        {
            OnVerify = OnlyFor(authority, new ZarinPalVerifyResult { IsSuccessful = true, RefId = 987654, CardPan = "6274********1234", Fee = 500 })
        };

        using (var scope = Factory.Services.CreateScope())
            await NewReconciler(scope, gateway).ReconcileStalePendingAsync(TimeSpan.Zero);

        var after = await LoadAsync(authority);
        after!.Status.Should().Be(PaymentStatus.Paid);
        after.RefNumber.Should().Be("987654");
        after.Transactions.Should().Contain(t => t.Type == TransactionType.Verification, // never untraceable
            "convergence must leave an auditable verification transaction");
    }

    [Fact]
    public async Task Converges_stale_pending_to_Failed_when_gateway_reports_not_verified()
    {
        var (_, authority) = await SeedStalePendingAsync();
        var gateway = new FakeZarinPal
        {
            OnVerify = OnlyFor(authority, new ZarinPalVerifyResult { IsSuccessful = false, ErrorCode = -51, ErrorMessage = "not paid" })
        };

        using (var scope = Factory.Services.CreateScope())
            await NewReconciler(scope, gateway).ReconcileStalePendingAsync(TimeSpan.Zero);

        var after = await LoadAsync(authority);
        after!.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task Gateway_timeout_leaves_payment_Pending_for_the_next_sweep_and_does_not_crash()
    {
        // Fault injection: gateway unreachable / times out. Money is NOT lost — the payment stays Pending and is
        // retried on the next sweep. The sweep must swallow the error and continue.
        var (_, authority) = await SeedStalePendingAsync();
        var gateway = new FakeZarinPal
        {
            OnVerify = (_, _) => throw new TaskCanceledException("gateway timeout")
        };

        Func<Task> sweep = async () =>
        {
            using var scope = Factory.Services.CreateScope();
            await NewReconciler(scope, gateway).ReconcileStalePendingAsync(TimeSpan.Zero);
        };
        await sweep.Should().NotThrowAsync("the sweep must swallow a gateway failure and continue");

        var after = await LoadAsync(authority);
        after!.Status.Should().Be(PaymentStatus.Pending, "the payment remains recoverable — never lost");
    }

    [Fact]
    public async Task Reconciliation_is_idempotent_a_second_sweep_does_not_re_settle()
    {
        // Duplicate-callback / re-run safety: once converged to Paid, further sweeps must not touch it (I2: never duplicated).
        var (_, authority) = await SeedStalePendingAsync();
        var gateway = new FakeZarinPal
        {
            OnVerify = OnlyFor(authority, new ZarinPalVerifyResult { IsSuccessful = true, RefId = 111, Fee = 0 })
        };

        using (var scope = Factory.Services.CreateScope())
            await NewReconciler(scope, gateway).ReconcileStalePendingAsync(TimeSpan.Zero);
        using (var scope = Factory.Services.CreateScope())
            await NewReconciler(scope, gateway).ReconcileStalePendingAsync(TimeSpan.Zero);

        var after = await LoadAsync(authority);
        after!.Status.Should().Be(PaymentStatus.Paid);
        after.Transactions.Count(t => t.Type == TransactionType.Verification).Should().Be(1,
            "exactly one settlement — the second sweep must not re-settle an already-Paid payment");
    }
}

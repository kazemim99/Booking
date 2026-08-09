using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Payments;

/// <summary>
/// C2 payment-consistency, invariant I2 (money never duplicated): the partial unique
/// index UX_Payments_OneCapturedPerBooking guarantees at most one CAPTURED payment per
/// booking at the database layer — even under concurrent retries — while still allowing
/// Pending/Failed re-attempts. Validated at the raw-SQL layer so it isolates the DB
/// constraint from EF aggregate/owned-entity concerns.
/// </summary>
public class PaymentDedupTests : ServiceCatalogIntegrationTestBase
{
    public PaymentDedupTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    private async Task InsertPaymentAsync(Guid bookingId, string status)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""ServiceCatalog"".""Payments""
              (""PaymentId"",""BookingId"",""CustomerId"",""ProviderId"",""Amount"",""Currency"",
               ""PaidAmount"",""PaidCurrency"",""RefundedAmount"",""RefundedCurrency"",
               ""Status"",""Method"",""Provider"",""CreatedAt"",""Metadata"",""IsDeleted"")
              VALUES ({0},{1},{2},{3},100,'USD',100,'USD',0,'USD',{4},'CreditCard','ZarinPal',now(),{5}::jsonb,false)",
            Guid.NewGuid(), bookingId, Guid.NewGuid(), Guid.NewGuid(), status, "{}");
    }

    private async Task<int> CapturedCountAsync(Guid bookingId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var rows = await db.Database.SqlQueryRaw<int>(
            @"SELECT COUNT(*)::int AS ""Value"" FROM ""ServiceCatalog"".""Payments""
              WHERE ""BookingId"" = {0} AND ""Status"" IN ('Paid','PartiallyPaid')", bookingId)
            .ToListAsync();
        return rows[0];
    }

    [Fact]
    public async Task Second_captured_payment_for_a_booking_is_rejected_but_pending_and_failed_are_allowed()
    {
        var bookingId = Guid.NewGuid();

        await InsertPaymentAsync(bookingId, "Paid"); // first captured payment — ok

        var second = () => InsertPaymentAsync(bookingId, "Paid");
        (await second.Should().ThrowAsync<Exception>("a booking may have at most one captured payment"))
            .Which.ToString().Should().Contain("UX_Payments_OneCapturedPerBooking");

        // Non-captured re-attempts for the same booking remain allowed (non-regressive).
        var pending = () => InsertPaymentAsync(bookingId, "Pending");
        await pending.Should().NotThrowAsync("a Pending re-attempt must be allowed");
        var failed = () => InsertPaymentAsync(bookingId, "Failed");
        await failed.Should().NotThrowAsync("a Failed attempt must not block");

        (await CapturedCountAsync(bookingId)).Should().Be(1);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(22)]
    public async Task Concurrent_captured_payments_for_one_booking_yield_exactly_one(int _)
    {
        var bookingId = Guid.NewGuid();

        async Task TryCaptured()
        {
            try { await InsertPaymentAsync(bookingId, "Paid"); }
            catch { /* expected loser: unique-violation / serialization */ }
        }

        const int concurrency = 8;
        await Task.WhenAll(Enumerable.Range(0, concurrency).Select(_ => TryCaptured()));

        (await CapturedCountAsync(bookingId)).Should().Be(1,
            "exactly one captured payment may exist per booking, even under concurrent retries");
    }
}

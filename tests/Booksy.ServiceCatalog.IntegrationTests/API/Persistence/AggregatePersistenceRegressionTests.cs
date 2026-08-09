using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Persistence;

/// <summary>
/// Regression protection for <c>fix-aggregate-persistence-concurrency</c>.
///
/// Appending a new owned child entity (with a domain-generated GUID key) to a materialized aggregate and saving
/// used to throw <see cref="DbUpdateConcurrencyException"/> ("expected 1 row, affected 0"): EF classified the new
/// child as <c>Modified</c> and emitted a phantom UPDATE of a non-existent row. The fix maps those child keys
/// <c>ValueGeneratedNever()</c>, de-aliases owned <see cref="Money"/> instances, and makes repository
/// <c>UpdateAsync</c> a no-op on already-tracked aggregates. These tests lock in that behaviour for every affected
/// aggregate: Payment (Transactions), Notification (DeliveryAttempts) and Service (PriceTiers).
/// </summary>
public class AggregatePersistenceRegressionTests : Infrastructure.ServiceCatalogIntegrationTestBase
{
    public AggregatePersistenceRegressionTests(
        Infrastructure.ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    // ---------------------------------------------------------------- Payment.Transactions

    [Fact]
    public async Task Verifying_a_pending_payment_appends_a_transaction_and_persists()
    {
        var authority = $"auth-{Guid.NewGuid():N}";
        Guid id;
        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
            var p = Payment.CreateForBooking(
                BookingId.From(Guid.NewGuid()), UserId.From(Guid.NewGuid()), ProviderId.From(Guid.NewGuid()),
                Money.Create(100, "USD"), PaymentMethod.ZarinPal);
            p.RecordPaymentRequest(authority, "https://pay.test/authority");
            id = p.Id.Value;
            await repo.AddAsync(p);
            await uow.CommitAsync();
        }

        // Tracked load -> domain method that appends a Verification Transaction -> commit (production verify path).
        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
            var loaded = await repo.GetByAuthorityAsync(authority);
            loaded!.VerifyPayment("REF-1", "6274********1234", 500);

            var commit = async () => await uow.CommitAsync();
            await commit.Should().NotThrowAsync<DbUpdateConcurrencyException>();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>();
            var after = await repo.GetByIdAsync(PaymentId.From(id));
            after!.Status.Should().Be(PaymentStatus.Paid);
            after.Transactions.Should().Contain(t => t.Type == TransactionType.Verification);
        }
    }

    [Fact]
    public async Task Refund_flow_tracked_load_plus_UpdateAsync_persists()
    {
        var authority = $"auth-{Guid.NewGuid():N}";
        Guid id;
        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
            var p = Payment.CreateForBooking(
                BookingId.From(Guid.NewGuid()), UserId.From(Guid.NewGuid()), ProviderId.From(Guid.NewGuid()),
                Money.Create(100, "USD"), PaymentMethod.ZarinPal);
            p.RecordPaymentRequest(authority, "https://pay.test/authority");
            p.VerifyPayment("REF-INITIAL", "6274********0000", 500); // -> Paid
            id = p.Id.Value;
            await repo.AddAsync(p);
            await uow.CommitAsync();
        }

        // Mirrors RefundPaymentCommandHandler: tracked GetById -> Refund (appends Transaction) -> UpdateAsync -> commit.
        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
            var loaded = await repo.GetByIdAsync(PaymentId.From(id));
            loaded!.Refund(Money.Create(100, "USD"), "RFND-1", RefundReason.CustomerCancellation, "test");
            await repo.UpdateAsync(loaded);

            var commit = async () => await uow.CommitAsync();
            await commit.Should().NotThrowAsync<DbUpdateConcurrencyException>();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>();
            var after = await repo.GetByIdAsync(PaymentId.From(id));
            after!.Status.Should().Be(PaymentStatus.Refunded);
            after.Transactions.Should().Contain(t => t.Type == TransactionType.Refund);
        }
    }

    // ---------------------------------------------------------------- Notification.DeliveryAttempts

    [Fact]
    public async Task Sending_a_notification_appends_a_delivery_attempt_and_persists()
    {
        Guid id;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var n = Notification.CreateImmediate(
                UserId.From(Guid.NewGuid()), NotificationType.BookingConfirmation, NotificationChannel.Email,
                "Subject", "Body", NotificationPriority.Normal, recipientEmail: "to@test.com");
            id = n.Id.Value;
            db.Notifications.Add(n);
            await db.SaveChangesAsync();
        }

        // Tracked load -> Send() appends a DeliveryAttempt -> save. This threw DbUpdateConcurrencyException before the fix.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var n = await db.Notifications.Include(x => x.DeliveryAttempts)
                .FirstAsync(x => x.Id == NotificationId.From(id));
            n.Send();

            var save = async () => await db.SaveChangesAsync();
            await save.Should().NotThrowAsync<DbUpdateConcurrencyException>();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var n = await db.Notifications.Include(x => x.DeliveryAttempts)
                .AsNoTracking().FirstAsync(x => x.Id == NotificationId.From(id));
            n.Status.Should().Be(NotificationStatus.Sent);
            n.DeliveryAttempts.Should().HaveCount(1);
        }
    }

    // ---------------------------------------------------------------- Service.PriceTiers

    [Fact]
    public async Task Adding_a_price_tier_to_an_existing_service_persists()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("Svc Provider", "svc-provider@test.com");

        Guid id;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var svc = Service.Create(
                provider.Id, "Svc", "Desc", ServiceCategory.HairSalon,
                ServiceType.Standard, Price.Create(50, "USD"), Duration.FromMinutes(60));
            id = svc.Id.Value;
            db.Services.Add(svc);
            await db.SaveChangesAsync();
        }

        // Tracked load -> AddPriceTier appends a PriceTier (client GUID key) -> save.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var svc = await db.Services.Include(s => s.PriceTiers).FirstAsync(s => s.Id == ServiceId.From(id));
            svc.AddPriceTier("Premium", Price.Create(80, "USD"), "premium tier");

            var save = async () => await db.SaveChangesAsync();
            await save.Should().NotThrowAsync<DbUpdateConcurrencyException>();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var svc = await db.Services.Include(s => s.PriceTiers).AsNoTracking()
                .FirstAsync(s => s.Id == ServiceId.From(id));
            svc.PriceTiers.Should().Contain(t => t.Name == "Premium");
        }
    }
}

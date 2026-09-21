using System.Net.Http.Json;
using Booksy.Host.IntegrationTests.Infrastructure.Fakes;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// The customer is told when their money did NOT move.
/// </summary>
/// <remarks>
/// <para>This notification was blocked for two days and the reason is worth keeping: the integration tests'
/// payment gateway always succeeded, so a failure could not be provoked through the API and the failing test
/// could not be written first. Rather than wire the notification blind, the fake gained an opt-in decline
/// (<see cref="FakePaymentGateway.DeclineMetadataKey"/>) — tasks.md 7.8.</para>
///
/// <para>The decline is requested per payment, through the request's own metadata, rather than by flipping a
/// switch on the shared fake. Two test collections run against one host in parallel; a mutable "fail the next
/// call" flag would be a race that fails somebody else's payment.</para>
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class PaymentFailureNotificationTests : ServiceCatalogIntegrationTestBase
{
    public PaymentFailureNotificationTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task A_declined_payment_tells_the_customer()
    {
        var arranged = await ArrangeAsync();

        await PayAsync(arranged, decline: true);

        (await RaisedForAsync(arranged.CustomerId))
            .Should().Contain(NotificationEventCode.PaymentFailed);
    }

    [Fact]
    public async Task A_declined_payment_produces_exactly_one_notification()
    {
        // PaymentFailedNotificationHandler was live and sending English HTML off the domain event. Payment
        // events DO dispatch — CommitAsync delegates to SaveChangesAsync — so it had to be deleted in the
        // same step that raised this through the outbox, or a customer whose card was declined would get
        // told twice, in two languages.
        var arranged = await ArrangeAsync();

        await PayAsync(arranged, decline: true);

        (await RaisedForAsync(arranged.CustomerId))
            .Count(c => c == NotificationEventCode.PaymentFailed)
            .Should().Be(1);

        (await NonOutboxNotificationCountAsync(arranged.CustomerId)).Should().Be(
            0, "nothing outside the outbox should be notifying about a payment");
    }

    [Fact]
    public async Task A_payment_that_went_through_is_not_reported_as_failed()
    {
        var arranged = await ArrangeAsync();

        await PayAsync(arranged, decline: false);

        (await RaisedForAsync(arranged.CustomerId))
            .Should().NotContain(NotificationEventCode.PaymentFailed);
    }

    [Fact]
    public async Task The_gateway_only_declines_when_a_payment_asks_it_to()
    {
        // 7.8's own property, and the guard on every other payment test in this suite: the failure mode is
        // opt-in per request. If it ever leaked into the default path, a few dozen unrelated tests would
        // start failing for reasons that have nothing to do with what they assert.
        var arranged = await ArrangeAsync();

        await PayAsync(arranged, decline: false);

        var payment = await FindPaymentForBookingAsync(arranged.BookingId);
        payment.Should().NotBeNull();
        payment!.Status.Should().NotBe(PaymentStatus.Failed);
    }

    // ── arrange ──

    private sealed record Arranged(Guid BookingId, Guid CustomerId, Guid ProviderId);

    private async Task<Arranged> ArrangeAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = (await GetProviderServicesAsync(provider.Id.Value)).First();
        var customerId = Guid.NewGuid();

        var booking = Domain.Aggregates.BookingAggregate.Booking.CreateBookingRequest(
            Core.Domain.ValueObjects.UserId.From(customerId),
            provider.Id,
            service.Id,
            provider.Id.Value,
            DateTime.UtcNow.AddDays(2),
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? Domain.ValueObjects.BookingPolicy.Default,
            "payment failure");

        await CreateEntityAsync(booking);

        return new Arranged(booking.Id.Value, customerId, provider.Id.Value);
    }

    /// <summary>
    /// Posts the payment. A declined one answers 400 — the controller turns an unsuccessful result into a
    /// validation error — and that is not what is under test here: the notification is raised and committed
    /// inside the handler, before the controller ever sees the result.
    /// </summary>
    private async Task PayAsync(Arranged arranged, bool decline)
    {
        AuthenticateAsUser(arranged.CustomerId, "customer@test.com");

        var metadata = decline
            ? new Dictionary<string, string>
            {
                [FakePaymentGateway.DeclineMetadataKey] = FakePaymentGateway.DeclineMetadataValue,
            }
            : new Dictionary<string, string>();

        await Client.PostAsJsonAsync("/api/v1/Payments", new
        {
            bookingId = arranged.BookingId,
            providerId = arranged.ProviderId,
            amount = 100m,
            currency = "IRT",
            paymentMethod = "CreditCard",
            paymentMethodId = "pm_test_card",
            description = "پرداخت آزمایشی",
            metadata,
        });
    }

    private async Task<Domain.Aggregates.PaymentAggregate.Payment?> FindPaymentForBookingAsync(Guid bookingId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var typed = Domain.ValueObjects.BookingId.From(bookingId);
        return await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == typed);
    }

    private async Task<List<NotificationEventCode>> RaisedForAsync(Guid recipientId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await db.NotificationOutbox.AsNoTracking()
            .Where(e => e.RecipientId == recipientId)
            .Select(e => e.EventCode)
            .ToListAsync();
    }

    /// <summary>
    /// Notifications not produced by the outbox. Distinguished by event code, because the sweep runs on a
    /// timer inside the test host and a slow test would otherwise count its own work.
    /// </summary>
    private async Task<int> NonOutboxNotificationCountAsync(Guid recipientId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var typed = Core.Domain.ValueObjects.UserId.From(recipientId);
        return await db.Notifications.AsNoTracking()
            .CountAsync(n => n.RecipientId == typed && n.EventCode == null);
    }
}

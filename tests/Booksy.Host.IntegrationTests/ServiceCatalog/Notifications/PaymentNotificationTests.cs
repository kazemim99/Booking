using System.Net;
using System.Net.Http.Json;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// Money notifications: the customer is told what happened to their money.
/// </summary>
/// <remarks>
/// <para>These are the notifications a recipient is entitled to rather than merely interested in, which is
/// why the catalogue marks them critical and non-suppressible. A refund the customer is never told about is
/// indistinguishable, from their side, from a refund that never happened.</para>
///
/// <para>One test here asserts something about the OLD code on purpose: that the legacy payment event
/// handlers produce nothing. Payment commands are <c>INonTransactionalCommand</c> and commit with
/// <c>CommitAsync</c>, which does not dispatch — so those handlers cannot run. That is a claim about
/// runtime behaviour, and reading the code is how I got the dispatch story wrong once already.</para>
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class PaymentNotificationTests : ServiceCatalogIntegrationTestBase
{
    public PaymentNotificationTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Refunding_tells_the_customer()
    {
        var (paymentId, customerId) = await ArrangePaidPaymentAsync();

        AuthenticateAsUser(customerId, "customer@test.com");
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/Payments/{paymentId}/refund",
            new { amount = 100m, reason = "CustomerCancellation", notes = "تست" });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "refund failed: " + body);

        var raised = await RaisedForAsync(customerId);
        raised.Should().Contain(NotificationEventCode.RefundProcessed);
    }

    [Fact]
    public async Task The_refund_notice_is_addressed_to_the_customer_not_the_payer_of_record()
    {
        var (paymentId, customerId) = await ArrangePaidPaymentAsync();

        AuthenticateAsUser(customerId, "customer@test.com");
        await Client.PostAsJsonAsync(
            $"/api/v1/Payments/{paymentId}/refund",
            new { amount = 100m, reason = "CustomerCancellation", notes = "تست" });

        var rows = await OutboxForAsync(NotificationEventCode.RefundProcessed);
        rows.Should().OnlyContain(r => r.RecipientId == customerId);
    }

    [Fact]
    public async Task A_refund_produces_exactly_one_notification_not_two()
    {
        // Payment domain events DO dispatch — CommitAsync delegates to SaveChangesAsync, which dispatches,
        // so the legacy PaymentRefundedNotificationHandler was live. With the outbox also raising, the
        // customer would have received two refund notices: one Persian, one English. The legacy handler is
        // deleted; this test is what stops it, or an equivalent, coming back.
        var (paymentId, customerId) = await ArrangePaidPaymentAsync();

        AuthenticateAsUser(customerId, "customer@test.com");
        await Client.PostAsJsonAsync(
            $"/api/v1/Payments/{paymentId}/refund",
            new { amount = 100m, reason = "CustomerCancellation", notes = "تست" });

        var refundIntents = (await RaisedForAsync(customerId))
            .Count(c => c == NotificationEventCode.RefundProcessed);

        refundIntents.Should().Be(1);
        (await LegacyNotificationCountAsync(customerId)).Should().Be(
            0, "nothing outside the outbox should be notifying about a refund");
    }

    // ── arrange ──

    /// <summary>
    /// A captured payment, built through the aggregate rather than inserted as raw SQL.
    /// </summary>
    /// <remarks>
    /// A hand-written INSERT produced a row the refund path could not load — the aggregate owns state a
    /// column list does not capture, and the failure surfaced as a 500 rather than as anything that pointed
    /// at the arrange. Using the factory is both shorter and the only version that stays correct when the
    /// mapping changes.
    /// </remarks>
    private async Task<(Guid PaymentId, Guid CustomerId)> ArrangePaidPaymentAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = (await DbContext.Services
            .Where(s => s.ProviderId == provider.Id)
            .ToListAsync()).First();

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
            "payment notification");

        await CreateEntityAsync(booking);

        var payment = Domain.Aggregates.PaymentAggregate.Payment.CreateForBooking(
            booking.Id,
            Core.Domain.ValueObjects.UserId.From(customerId),
            provider.Id,
            Core.Domain.ValueObjects.Money.Create(100, "IRT"),
            PaymentMethod.CreditCard);

        payment.ProcessCharge($"pi_test_{Guid.NewGuid()}", "pm_test_card");
        await CreateEntityAsync(payment);

        return (payment.Id.Value, customerId);
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

    private async Task<List<(Guid RecipientId, NotificationEventCode Code)>> OutboxForAsync(
        NotificationEventCode code)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var rows = await db.NotificationOutbox.AsNoTracking()
            .Where(e => e.EventCode == code)
            .Select(e => new { e.RecipientId, e.EventCode })
            .ToListAsync();
        return rows.Select(r => (r.RecipientId, r.EventCode)).ToList();
    }

    /// <summary>
    /// Notifications for a recipient that did NOT come from the outbox.
    /// </summary>
    /// <remarks>
    /// The distinction is the event code: everything the outbox produces carries one, and the legacy
    /// handlers cannot set one. Counting every notification instead would also count the ones the hosted
    /// outbox sweep creates — it runs every 15 seconds inside the test host, so a slow test sees its own
    /// work and reads it as the legacy handler's.
    /// </remarks>
    private async Task<List<string>> LegacyRowsAsync(Guid recipientId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var typed = Core.Domain.ValueObjects.UserId.From(recipientId);
        var rows = await db.Notifications.AsNoTracking()
            .Where(n => n.RecipientId == typed)
            .Select(n => new { n.Type, n.Subject, n.EventCode, n.Status })
            .ToListAsync();
        return rows.Select(r => $"{r.Type}/{r.EventCode}/{r.Status}/{r.Subject}").ToList();
    }

    private async Task<int> LegacyNotificationCountAsync(Guid recipientId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        // Compared as the strongly-typed id: the value converter means `.Value` has no SQL translation.
        var typed = Core.Domain.ValueObjects.UserId.From(recipientId);
        return await db.Notifications.AsNoTracking()
            .CountAsync(n => n.RecipientId == typed && n.EventCode == null);
    }
}

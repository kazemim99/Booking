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
/// The customer is told when their money is taken.
/// </summary>
/// <remarks>
/// Paired with <c>PaymentNotificationTests</c>, which covers the refund. Both are non-suppressible in the
/// catalogue for the same reason: they are records the customer is entitled to, not messages they opted
/// into.
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class PaymentCaptureNotificationTests : ServiceCatalogIntegrationTestBase
{
    public PaymentCaptureNotificationTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Capturing_a_payment_tells_the_customer()
    {
        var (paymentId, customerId) = await ArrangePendingPaymentAsync();

        AuthenticateAsUser(customerId, "customer@test.com");
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/Payments/{paymentId}/capture",
            new { amount = 100m });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "capture failed: " + body);

        var raised = await RaisedForAsync(customerId);
        raised.Should().Contain(NotificationEventCode.PaymentReceived);
    }

    [Fact]
    public async Task A_captured_payment_produces_exactly_one_notification()
    {
        // The legacy PaymentProcessedNotificationHandler is live — payment events DO dispatch, because
        // CommitAsync delegates to SaveChangesAsync. It has to go in the same step that replaces it, or the
        // customer gets a Persian notice and an English one.
        var (paymentId, customerId) = await ArrangePendingPaymentAsync();

        AuthenticateAsUser(customerId, "customer@test.com");
        await Client.PostAsJsonAsync($"/api/v1/Payments/{paymentId}/capture", new { amount = 100m });

        (await RaisedForAsync(customerId))
            .Count(c => c == NotificationEventCode.PaymentReceived)
            .Should().Be(1);

        (await NonOutboxNotificationCountAsync(customerId)).Should().Be(
            0, "nothing outside the outbox should be notifying about a payment");
    }

    // ── arrange ──

    /// <summary>An authorised payment, built through its aggregate. Capture needs both.</summary>
    private async Task<(Guid PaymentId, Guid CustomerId)> ArrangePendingPaymentAsync()
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
            "capture notification");

        await CreateEntityAsync(booking);

        var payment = Domain.Aggregates.PaymentAggregate.Payment.CreateForBooking(
            booking.Id,
            Core.Domain.ValueObjects.UserId.From(customerId),
            provider.Id,
            Core.Domain.ValueObjects.Money.Create(100, "IRT"),
            PaymentMethod.CreditCard);

        // Capture is only legal once the payment carries a gateway authorisation.
        payment.Authorize($"pi_test_{Guid.NewGuid()}", "pm_test_card");
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

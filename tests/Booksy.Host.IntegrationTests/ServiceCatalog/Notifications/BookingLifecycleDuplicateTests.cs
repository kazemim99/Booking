using System.Net;
using System.Net.Http.Json;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.ServiceCatalog.IntegrationTests.API.Bookings;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// That a booking's lifecycle notifies each person once, not twice.
/// </summary>
/// <remarks>
/// <para><b>The risk here is duplication, not absence.</b> Four legacy handlers
/// (<c>BookingConfirmedNotificationHandler</c> and its cancelled / no-show / rescheduled siblings) send
/// English HTML off the domain event, while the command handlers now raise the same notifications in Persian
/// through the outbox. Both paths are live: ServiceCatalog dispatches domain events, so "the old handler is
/// probably dead" is exactly the assumption that nearly shipped two refund notices (see tasks.md 7.2).</para>
///
/// <para>So these assert a COUNT, and they count notifications that did not come from the outbox — a row
/// with no event code cannot have been raised by it. Asserting the outbox row exists would pass happily
/// while the customer receives two messages.</para>
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class BookingLifecycleDuplicateTests : ServiceCatalogIntegrationTestBase
{
    public BookingLifecycleDuplicateTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Cancelling_notifies_through_the_outbox_only()
    {
        var b = await ArrangeAsync();

        AuthenticateAsUser(b.CustomerId, "customer@test.com");
        var response = await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/cancel",
            new CancelBookingRequest { Reason = "برنامه‌ام عوض شد", CancelledBy = b.CustomerId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await AssertNothingOutsideTheOutboxAsync(b);
    }

    [Fact]
    public async Task Confirming_notifies_through_the_outbox_only()
    {
        // Needs a booking that requires a deposit. Not a detail of this test — it is the only way through
        // the endpoint: PaymentMethodId is [Required] on the request, and the handler processes a deposit
        // whenever one is supplied, so confirming a deposit-free booking throws "No deposit required" and
        // answers 500. Found here, filed as FOLLOW-UPS #64; it is a booking defect, not a notification one.
        var b = await ArrangeAsync(withDeposit: true);

        AuthenticateAsProviderOwner(b.Provider);
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/bookings/{b.BookingId}/confirm",
            new { paymentMethodId = "pm_test_card" });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "confirm failed: " + body);

        await AssertNothingOutsideTheOutboxAsync(b);
    }

    [Fact]
    public async Task Marking_a_no_show_notifies_through_the_outbox_only()
    {
        var b = await ArrangeAsync(past: true);

        AuthenticateAsProviderOwner(b.Provider);
        var response = await PostAsJsonAsync<MarkNoShowRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/no-show",
            new MarkNoShowRequest { Notes = "مراجعه نشد" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await AssertNothingOutsideTheOutboxAsync(b);
    }

    [Fact]
    public async Task Rescheduling_notifies_through_the_outbox_only()
    {
        var b = await ArrangeAsync();

        AuthenticateAsUser(b.CustomerId, "customer@test.com");
        var response = await PostAsJsonAsync<RescheduleBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/reschedule",
            new RescheduleBookingRequest
            {
                NewStartTime = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(6), 14),
                Reason = "جابه‌جایی",
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await AssertNothingOutsideTheOutboxAsync(b);
    }

    // ── assert ──

    /// <summary>
    /// No notification reached either party except through the outbox.
    /// </summary>
    /// <remarks>
    /// Distinguished by event code: the outbox stamps one on everything it sends, and nothing else can.
    /// The sweep runs on a timer inside the test host, so a slow test sees its own rows arrive — counting
    /// rows without this distinction would count the correct notification as a duplicate of itself.
    /// </remarks>
    private async Task AssertNothingOutsideTheOutboxAsync(Arranged b)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();

        var customer = UserId.From(b.CustomerId);
        var owner = UserId.From(b.OwnerId);

        var strays = await db.Notifications.AsNoTracking()
            .Where(n => (n.RecipientId == customer || n.RecipientId == owner) && n.EventCode == null)
            .Select(n => n.Subject)
            .ToListAsync();

        strays.Should().BeEmpty(
            "every notification about a booking should come from the outbox; anything else is a second, "
            + "English copy of a message the customer already got in Persian");
    }

    // ── arrange ──

    private sealed record Arranged(Guid BookingId, Guid CustomerId, Guid OwnerId, Domain.Aggregates.Provider Provider);

    private async Task<Arranged> ArrangeAsync(bool past = false, bool withDeposit = false)
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = (await GetProviderServicesAsync(provider.Id.Value)).First();
        var customerId = Guid.NewGuid();

        var policy = withDeposit ? DepositRequiringPolicy : service.BookingPolicy ?? BookingPolicy.Default;

        var start = past
            ? DateTime.UtcNow.AddHours(-3)
            : NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(4), 14);

        // A past booking has to be confirmed on creation — Confirm() demands two hours' notice, and
        // MarkAsNoShow() demands the appointment be over. Nothing satisfies both through request-then-confirm.
        var booking = past
            ? Booking.CreateConfirmedByProvider(
                UserId.From(customerId), provider.Id, service.Id, provider.Id.Value, start,
                service.Duration, service.BasePrice, policy, "dup")
            : Booking.CreateBookingRequest(
                UserId.From(customerId), provider.Id, service.Id, provider.Id.Value, start,
                service.Duration, service.BasePrice, policy, "dup");

        await CreateEntityAsync(booking);

        return new Arranged(booking.Id.Value, customerId, provider.OwnerId.Value, provider);
    }

    /// <summary>Same as the default policy, except that it asks for a 20% deposit.</summary>
    private static BookingPolicy DepositRequiringPolicy => BookingPolicy.Create(
        minAdvanceBookingHours: 2,
        maxAdvanceBookingDays: 90,
        cancellationWindowHours: 24,
        cancellationFeePercentage: 50,
        allowRescheduling: true,
        rescheduleWindowHours: 24,
        requireDeposit: true,
        depositPercentage: 20);

    private static DateTime NextWeekdayAtHour(DateTime from, int hour)
    {
        var day = from.Date;
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day.AddHours(hour);
    }
}

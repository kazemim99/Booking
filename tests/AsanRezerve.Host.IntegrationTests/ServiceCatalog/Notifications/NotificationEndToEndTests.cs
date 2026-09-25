using System.Net;
using AsanRezerve.ServiceCatalog.Api.Models.Responses;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Infrastructure.BackgroundJobs;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Notifications;
using AsanRezerve.ServiceCatalog.IntegrationTests.API.Bookings;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// The whole chain, from a booking somebody actually made to a notification they can actually read.
/// </summary>
/// <remarks>
/// <para>Every other test in this area starts partway along — at the raiser, or at the scheduler. This one
/// starts at <c>POST /api/v1/bookings</c> and ends at the customer's inbox, so the seams between the parts
/// are what it covers: the command handler raising an intent, the intent surviving the commit, the sweep
/// turning it into a notification, the dispatcher sending it, and the inbox showing it to the right
/// person.</para>
///
/// <para>Those seams are exactly what unit tests cannot see, and where this feature has already broken
/// twice — once because the handlers never set recipient contact, and once because the delivery path was
/// wired to a stub that reported success without sending.</para>
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class NotificationEndToEndTests : ServiceCatalogIntegrationTestBase
{
    public NotificationEndToEndTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task A_booking_made_through_the_api_reaches_the_customers_inbox()
    {
        var (customerId, _) = await BookThroughTheApiAsync();

        await SweepAsync();

        AuthenticateAsUser(customerId, "customer@test.com");
        var inbox = await InboxAsync();

        inbox["items"]!.Should().NotBeEmpty(
            "a customer who books should be told something about it");
    }

    [Fact]
    public async Task The_notification_the_customer_receives_is_in_persian()
    {
        var (customerId, _) = await BookThroughTheApiAsync();
        await SweepAsync();

        AuthenticateAsUser(customerId, "customer@test.com");
        var item = (await InboxAsync())["items"]![0]!;

        // The audience is Iranian. An English notification is a defect, not a cosmetic issue.
        item["body"]!.Value<string>().Should().MatchRegex("[؀-ۿ]");
        item["subject"]!.Value<string>().Should().MatchRegex("[؀-ۿ]");
    }

    [Fact]
    public async Task The_notification_carries_its_event_code_and_points_at_the_booking()
    {
        var (customerId, bookingId) = await BookThroughTheApiAsync();
        await SweepAsync();

        AuthenticateAsUser(customerId, "customer@test.com");
        var item = (await InboxAsync())["items"]![0]!;

        item["eventCode"]!.Value<string>().Should().NotBeNullOrWhiteSpace(
            "the client selects its presentation from the code");
        item["destinationId"]!.Value<string>().Should().Be(bookingId.ToString());
        item["isActionable"]!.Value<bool>().Should().BeTrue(
            "the booking exists and belongs to this customer");
    }

    [Fact]
    public async Task Sweeping_twice_does_not_notify_twice()
    {
        var (customerId, _) = await BookThroughTheApiAsync();

        await SweepAsync();
        await SweepAsync();

        AuthenticateAsUser(customerId, "customer@test.com");
        var inbox = await InboxAsync();

        inbox["items"]!.Children().Should().HaveCount(
            1, "the outbox row is processed once, however often the sweep runs");
    }

    [Fact]
    public async Task Nothing_is_delivered_before_the_sweep_runs()
    {
        // Proves the notification really does travel through the outbox rather than being sent inline by
        // the request — which is the whole point of the design.
        var (customerId, _) = await BookThroughTheApiAsync();

        AuthenticateAsUser(customerId, "customer@test.com");
        var inbox = await InboxAsync();

        inbox["items"]!.Should().BeEmpty();
    }

    [Fact]
    public async Task The_outbox_row_is_marked_processed_rather_than_left_behind()
    {
        var (_, bookingId) = await BookThroughTheApiAsync();

        await SweepAsync();

        var states = await OutboxStatesForBookingAsync(bookingId);
        states.Should().NotBeEmpty();
        states.Should().NotContain(NotificationOutboxState.Pending);
    }

    // ── arrange ──

    /// <summary>
    /// Books the way a customer does: through the API, against a real provider, service and staff member.
    /// </summary>
    private async Task<(Guid CustomerId, Guid BookingId)> BookThroughTheApiAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = (await DbContext.Services
            .Where(s => s.ProviderId == provider.Id)
            .ToListAsync()).First();

        var customerId = Guid.NewGuid();
        AuthenticateAsUser(customerId, "customer@test.com");

        // The fixture's business hours are 09:00-17:00 every day, so any weekday morning is inside them.
        var now = DateTime.UtcNow;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7;
        var startTime = now.AddDays(daysUntilMonday).Date.AddHours(10);

        var response = await PostAsJsonAsync<CreateBookingRequest, BookingResponse>(
            "/api/v1/bookings",
            new CreateBookingRequest
            {
                ProviderId = provider.Id.Value,
                ServiceId = service.Id.Value,
                StaffId = provider.Id,
                StartTime = startTime,
                CustomerNotes = "end to end",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created, "the arrange must produce a real booking");
        return (customerId, response.Data!.Id);
    }

    private async Task SweepAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<ProcessNotificationOutboxJob>();
        await job.ExecuteAsync();
    }

    private async Task<JObject> InboxAsync()
    {
        var response = await Client.GetAsync("/api/v1/Notifications/inbox");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var parsed = JObject.Parse(await response.Content.ReadAsStringAsync());
        return parsed["data"] as JObject ?? parsed;
    }

    private async Task<List<string>> OutboxStatesForBookingAsync(Guid bookingId)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await context.NotificationOutbox
            .AsNoTracking()
            .Where(e => e.SubjectId == bookingId)
            .Select(e => e.State)
            .ToListAsync();
    }
}

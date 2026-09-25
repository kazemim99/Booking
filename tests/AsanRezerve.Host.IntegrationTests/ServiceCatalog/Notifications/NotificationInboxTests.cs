using System.Net;
using System.Net.Http.Json;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Infrastructure.BackgroundJobs;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// The inbox a person reads in the app: their own notifications, a badge count, and read-state.
/// </summary>
/// <remarks>
/// Driven through HTTP rather than the handlers, because the property that matters most is that every one of
/// these is scoped to the authenticated caller. That is an authorization claim, and testing it below the
/// controller would not prove it.
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class NotificationInboxTests : ServiceCatalogIntegrationTestBase
{
    public NotificationInboxTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task A_notification_raised_for_me_appears_in_my_inbox()
    {
        var me = Guid.NewGuid();
        await RaiseAndSweepAsync(NotificationEventCode.BookingConfirmed, me);

        AuthenticateAsUser(me);
        var inbox = await GetInboxAsync();

        inbox["items"]!.Should().NotBeEmpty();
        // Serialised camelCase by the API's JSON settings; what matters is that the code travels at all,
        // since the client selects its icon from this rather than from translated text.
        inbox["items"]![0]!["eventCode"]!.Value<string>()
            .Should().BeEquivalentTo(nameof(NotificationEventCode.BookingConfirmed));
    }

    [Fact]
    public async Task I_never_see_someone_elses_notifications()
    {
        var stranger = Guid.NewGuid();
        var me = Guid.NewGuid();
        await RaiseAndSweepAsync(NotificationEventCode.BookingConfirmed, stranger);

        AuthenticateAsUser(me);
        var inbox = await GetInboxAsync();

        inbox["items"]!.Should().BeEmpty();
    }

    [Fact]
    public async Task The_unread_count_reflects_what_i_have_not_opened()
    {
        var me = Guid.NewGuid();
        await RaiseAndSweepAsync(NotificationEventCode.BookingConfirmed, me);
        AuthenticateAsUser(me);

        var before = await GetUnreadCountAsync();
        before.Should().BeGreaterThan(0);

        var id = FirstNotificationIdAsync(await GetInboxAsync());
        (await Client.PostAsync($"/api/v1/Notifications/{id}/read", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        (await GetUnreadCountAsync()).Should().Be(before - 1);
    }

    [Fact]
    public async Task Marking_read_twice_is_harmless()
    {
        // Apps re-send this on every open; the second call must not be an error or double-count anything.
        var me = Guid.NewGuid();
        await RaiseAndSweepAsync(NotificationEventCode.BookingConfirmed, me);
        AuthenticateAsUser(me);

        var id = FirstNotificationIdAsync(await GetInboxAsync());

        (await Client.PostAsync($"/api/v1/Notifications/{id}/read", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await Client.PostAsync($"/api/v1/Notifications/{id}/read", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        (await GetUnreadCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task I_cannot_mark_someone_elses_notification_read()
    {
        var stranger = Guid.NewGuid();
        await RaiseAndSweepAsync(NotificationEventCode.BookingConfirmed, stranger);

        // Find the stranger's notification id as the stranger, then try it as somebody else.
        AuthenticateAsUser(stranger);
        var id = FirstNotificationIdAsync(await GetInboxAsync());

        AuthenticateAsUser(Guid.NewGuid());
        var response = await Client.PostAsync($"/api/v1/Notifications/{id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "answering 403 would confirm the id exists");
    }

    [Fact]
    public async Task I_cannot_read_someone_elses_delivery_status()
    {
        // This endpoint used to take a notification id alone, exposing the recipient's phone number and the
        // gateway's response to any signed-in caller.
        var stranger = Guid.NewGuid();
        await RaiseAndSweepAsync(NotificationEventCode.BookingConfirmed, stranger);

        AuthenticateAsUser(stranger);
        var id = FirstNotificationIdAsync(await GetInboxAsync());

        AuthenticateAsUser(Guid.NewGuid());
        var response = await Client.GetAsync($"/api/v1/Notifications/{id}/status");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Mark_all_clears_the_badge()
    {
        var me = Guid.NewGuid();
        await RaiseAndSweepAsync(NotificationEventCode.BookingConfirmed, me);
        await RaiseAndSweepAsync(NotificationEventCode.BookingReminder24h, me);

        AuthenticateAsUser(me);
        (await GetUnreadCountAsync()).Should().BeGreaterThan(0);

        var response = await Client.PostAsync("/api/v1/Notifications/read-all", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetUnreadCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task A_notification_whose_booking_is_gone_is_not_tappable()
    {
        // Its words are history and still display; only the link dies.
        var me = Guid.NewGuid();
        await RaiseAndSweepAsync(
            NotificationEventCode.BookingConfirmed,
            me,
            subjectType: "Booking",
            subjectId: Guid.NewGuid()); // a booking that does not exist

        AuthenticateAsUser(me);
        var item = (await GetInboxAsync())["items"]![0]!;

        item["isActionable"]!.Value<bool>().Should().BeFalse();
        item["subject"]!.Value<string>().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task An_outbox_intent_scheduled_for_the_future_is_not_in_my_inbox_yet()
    {
        // Safe by construction: a scheduled intent does not become a Notification until it is due.
        var me = Guid.NewGuid();
        await RaiseAsync(NotificationEventCode.BookingReminder24h, me, scheduledFor: DateTime.UtcNow.AddDays(1));
        await SweepOnceAsync();

        AuthenticateAsUser(me);
        (await GetInboxAsync())["items"]!.Should().BeEmpty();
    }

    [Fact]
    public async Task A_queued_notification_that_has_not_been_sent_is_not_in_my_inbox()
    {
        // The legacy path (ScheduleNotificationCommand) writes a Notification row immediately, Queued, with
        // a future ScheduledFor. The inbox must not show it: nothing has reached this person yet, and
        // showing it would put a message in their list that no channel has delivered.
        var me = Guid.NewGuid();
        await QueueLegacyNotificationAsync(me, DateTime.UtcNow.AddHours(2));

        AuthenticateAsUser(me);
        (await GetInboxAsync())["items"]!.Should().BeEmpty();
    }

    [Fact]
    public async Task The_inbox_and_the_unread_count_agree()
    {
        // They read from different queries. If one filters on delivery state and the other does not, the
        // badge says 1 and the list shows 3 — and nobody can tell which is lying.
        var me = Guid.NewGuid();
        await RaiseAndSweepAsync(NotificationEventCode.BookingConfirmed, me);
        await QueueLegacyNotificationAsync(me, DateTime.UtcNow.AddHours(2));

        AuthenticateAsUser(me);

        var listed = (await GetInboxAsync())["items"]!.Children().Count();
        var unread = await GetUnreadCountAsync();

        listed.Should().Be(unread);
    }

    // ── helpers ──

    /// <summary>
    /// The id of the first inbox row. Read as a string and parsed: the id serialises as a JSON string, so
    /// asking Json.NET for a Guid directly throws an InvalidCastException.
    /// </summary>
    // QA walkthrough 2026-09-22: a salon's inbox showed "<h2>Your booking has been cancelled</h2>". Those rows were
    // written by the English HTML handlers deleted in f9502127; they carry no event code. They stay in the database
    // and simply stop being shown (and counted) — nothing current writes an HTML body.
    [Fact]
    public async Task A_legacy_english_html_notification_is_neither_shown_nor_counted()
    {
        var me = Guid.NewGuid();
        await WriteDeliveredLegacyRowAsync(me, "Booking Cancelled", "<h2>Your booking has been cancelled</h2><p>…</p>");

        AuthenticateAsUser(me);

        ((JArray)(await GetInboxAsync())["items"]!).Should().BeEmpty();
        (await GetUnreadCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task A_legacy_plain_text_notification_is_still_shown()
    {
        // Not a blanket purge of pre-outbox rows: only the HTML templates are unreadable.
        var me = Guid.NewGuid();
        await WriteDeliveredLegacyRowAsync(me, "اطلاعیه", "سالن فردا تعطیل است.");

        AuthenticateAsUser(me);

        ((JArray)(await GetInboxAsync())["items"]!).Should().ContainSingle();
        (await GetUnreadCountAsync()).Should().Be(1);
    }

    private async Task WriteDeliveredLegacyRowAsync(Guid recipientId, string subject, string body)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var row = Domain.Aggregates.NotificationAggregate.Notification.Create(
            AsanRezerve.Core.Domain.ValueObjects.UserId.From(recipientId),
            NotificationType.BookingCancelled,
            NotificationChannel.InApp,
            subject,
            body);
        row.Queue();
        row.MarkAsSent();
        context.Notifications.Add(row);
        await context.SaveChangesAsync();
    }

    private static Guid FirstNotificationIdAsync(JObject inbox) =>
        Guid.Parse(inbox["items"]![0]!["id"]!.Value<string>()!);

    /// <summary>Signs in as a specific user id, which is what every scoping assertion here turns on.</summary>
    private void AuthenticateAsUser(Guid userId) =>
        AuthenticateAs(TestUser.Customer(userId: userId));

    private async Task RaiseAsync(
        NotificationEventCode code,
        Guid recipientId,
        DateTime? scheduledFor = null)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var raiser = scope.ServiceProvider.GetRequiredService<INotificationRaiser>();

        await raiser.RaiseAsync(
            code,
            recipientId,
            Guid.NewGuid(),
            new Dictionary<string, string> { ["businessName"] = "سالن نهال" },
            scheduledFor: scheduledFor);

        await context.SaveChangesAsync();
    }

    /// <summary>Writes a Queued, future-dated notification the way the legacy schedule command does.</summary>
    private async Task QueueLegacyNotificationAsync(Guid recipientId, DateTime scheduledFor)
    {
        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();

        await mediator.Send(new Application.Commands.Notifications.ScheduleNotification.ScheduleNotificationCommand(
            RecipientId: recipientId,
            Type: NotificationType.ReviewRequest,
            Channel: NotificationChannel.InApp,
            Subject: "How was your experience?",
            Body: "<p>legacy</p>",
            ScheduledFor: scheduledFor));
    }

    private async Task SweepOnceAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<ProcessNotificationOutboxJob>();
        await job.ExecuteAsync();
    }

    private async Task RaiseAndSweepAsync(
        NotificationEventCode code,
        Guid recipientId,
        string? subjectType = null,
        Guid? subjectId = null)
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var raiser = scope.ServiceProvider.GetRequiredService<INotificationRaiser>();

            await raiser.RaiseAsync(
                code,
                recipientId,
                Guid.NewGuid(),
                new Dictionary<string, string> { ["businessName"] = "سالن نهال" },
                subjectType,
                subjectId);

            await context.SaveChangesAsync();
        }

        // The host's own NotificationOutboxService sweeps every 15 s too, and the claim is FOR UPDATE SKIP LOCKED: when
        // its pass has just claimed this row, our sweep steps over it and returns at once, and the test read the inbox
        // before that pass had sent it (the list said 0, the badge a moment later 1 — FULL verify, 2026-09-25). So sweep
        // until none of this recipient's due rows is still pending or claimed, whoever sends them.
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            using var scope = Factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ProcessNotificationOutboxJob>().ExecuteAsync();

            var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var now = DateTime.UtcNow;
            var inFlight = await context.NotificationOutbox.AsNoTracking().AnyAsync(o =>
                o.RecipientId == recipientId
                && (o.State == NotificationOutboxState.Pending || o.State == NotificationOutboxState.Claimed)
                && (o.ScheduledFor == null || o.ScheduledFor <= now));
            if (!inFlight)
                return;

            deadline.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10), "the raised notification should leave the outbox");
            await Task.Yield();
        }
    }

    /// <summary>
    /// Reads the inbox and unwraps the API's response envelope, which puts the payload under "data".
    /// </summary>
    private async Task<JObject> GetInboxAsync()
    {
        var response = await Client.GetAsync("/api/v1/Notifications/inbox");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return Unwrap(await response.Content.ReadAsStringAsync());
    }

    private static JObject Unwrap(string json)
    {
        var parsed = JObject.Parse(json);
        return parsed["data"] as JObject ?? parsed;
    }

    private async Task<int> GetUnreadCountAsync()
    {
        var response = await Client.GetAsync("/api/v1/Notifications/unread-count");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return Unwrap(await response.Content.ReadAsStringAsync())["unreadCount"]!.Value<int>();
    }
}

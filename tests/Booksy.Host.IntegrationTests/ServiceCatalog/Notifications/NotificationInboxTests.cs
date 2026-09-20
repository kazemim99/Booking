using System.Net;
using System.Net.Http.Json;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.BackgroundJobs;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// The inbox a person reads in the app: their own notifications, a badge count, and read-state.
/// </summary>
/// <remarks>
/// Driven through HTTP rather than the handlers, because the property that matters most is that every one of
/// these is scoped to the authenticated caller. That is an authorization claim, and testing it below the
/// controller would not prove it.
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class NotificationInboxTests : ServiceCatalogIntegrationTestBase
{
    public NotificationInboxTests(BooksyHostFactory factory) : base(factory)
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

    // ── helpers ──

    /// <summary>
    /// The id of the first inbox row. Read as a string and parsed: the id serialises as a JSON string, so
    /// asking Json.NET for a Guid directly throws an InvalidCastException.
    /// </summary>
    private static Guid FirstNotificationIdAsync(JObject inbox) =>
        Guid.Parse(inbox["items"]![0]!["id"]!.Value<string>()!);

    /// <summary>Signs in as a specific user id, which is what every scoping assertion here turns on.</summary>
    private void AuthenticateAsUser(Guid userId) =>
        AuthenticateAs(TestUser.Customer(userId: userId));

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

        using (var scope = Factory.Services.CreateScope())
        {
            var job = scope.ServiceProvider.GetRequiredService<ProcessNotificationOutboxJob>();
            await job.ExecuteAsync();
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

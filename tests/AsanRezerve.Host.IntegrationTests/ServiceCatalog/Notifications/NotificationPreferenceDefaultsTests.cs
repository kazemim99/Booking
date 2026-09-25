using System.Net.Http.Json;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// Saving one preference must not quietly change another.
/// </summary>
/// <remarks>
/// <para><b>The trap.</b> A person with no preferences row is sent on every channel — absent preferences are
/// never read as "disabled". But the first time they save ANY preference, a row is created, and it is built
/// from <c>NotificationPreference.Default</c>. That default was <c>Email | SMS | InApp</c>: written before push
/// existed and never updated. So the act of touching a settings screen switched push off, for good, without the
/// person ever seeing a push toggle.</para>
///
/// <para>It was harmless only because no client had ever called this endpoint. <c>add-notification-clients</c>
/// wires two screens to it, which would have sprung the trap on every user who saved a setting. These tests
/// were written first, and fixed before any screen was connected.</para>
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class NotificationPreferenceDefaultsTests : ServiceCatalogIntegrationTestBase
{
    public NotificationPreferenceDefaultsTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Saving_an_unrelated_preference_does_not_switch_push_off()
    {
        var userId = await SignedInWithNoPreferencesAsync();

        var response = await Client.PutAsJsonAsync(
            "/api/v1/notifications/preferences", new { marketingOptIn = true });
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        (await ChannelsAsync(userId)).Should().HaveFlag(
            NotificationChannel.PushNotification,
            "the person only changed their marketing opt-in; nothing asked for push to be switched off");
    }

    [Fact]
    public async Task A_person_who_saved_a_setting_is_still_sent_push()
    {
        // The behaviour that matters, stated through the policy that actually decides — not just the stored
        // flag. A suppressible notification, on push, to someone who has touched their settings once.
        var userId = await SignedInWithNoPreferencesAsync();

        await Client.PutAsJsonAsync("/api/v1/notifications/preferences", new { marketingOptIn = true });

        var preferences = await LoadAsync(userId);
        NotificationSuppressionPolicy.ShouldSend(
                preferences, NotificationChannel.PushNotification, NotificationType.BookingReminder)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Resetting_to_defaults_does_not_switch_push_off_either()
    {
        var userId = await SignedInWithNoPreferencesAsync();

        await Client.PostAsync("/api/v1/notifications/preferences/reset", null);

        (await ChannelsAsync(userId)).Should().HaveFlag(NotificationChannel.PushNotification);
    }

    [Fact]
    public async Task Switching_push_off_on_purpose_still_works()
    {
        // The fix must not make push impossible to turn off.
        var userId = await SignedInWithNoPreferencesAsync();

        await Client.PutAsJsonAsync("/api/v1/notifications/preferences", new
        {
            enabledChannels = (int)(NotificationChannel.Email | NotificationChannel.SMS | NotificationChannel.InApp),
        });

        (await ChannelsAsync(userId)).Should().NotHaveFlag(NotificationChannel.PushNotification);
    }

    [Fact]
    public async Task Reading_preferences_before_any_are_saved_reports_push_as_on()
    {
        // The same trap from the read side. The GET endpoint answers a person with no row with a hardcoded
        // default that ALSO omitted push — so a client that loads the screen and saves what it loaded would
        // switch push off by round-tripping. A screen shows what is true, and today push is sent to them.
        await SignedInWithNoPreferencesAsync();

        var response = await Client.GetAsync("/api/v1/notifications/preferences");
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);

        body.Should().Contain("PushNotification",
            "someone with no saved preferences is sent push, so the screen must not show it as off");
    }

    // ── arrange ──

    private Task<Guid> SignedInWithNoPreferencesAsync()
    {
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, $"prefs-{userId:N}@test.com");
        return Task.FromResult(userId);
    }

    private async Task<Domain.Aggregates.UserNotificationPreferencesAggregate.UserNotificationPreferences> LoadAsync(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserNotificationPreferencesRepository>();
        var preferences = await repo.GetByUserIdAsync(UserId.From(userId));
        preferences.Should().NotBeNull("saving a preference creates the row");
        return preferences!;
    }

    private async Task<NotificationChannel> ChannelsAsync(Guid userId) =>
        (await LoadAsync(userId)).Preferences.EnabledChannels;
}

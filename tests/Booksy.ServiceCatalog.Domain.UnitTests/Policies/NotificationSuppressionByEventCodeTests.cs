using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Policies;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.Policies;

/// <summary>
/// Whether a recipient's channel preferences can silence a notification.
/// </summary>
/// <remarks>
/// <para>Notifications raised through the outbox carry a catalogue code, and the catalogue is then the
/// authority. Everything else keeps the older behaviour driven by <see cref="NotificationType"/>, which is
/// pinned here so the new path cannot quietly change what existing callers do.</para>
///
/// <para>The reason the code matters at all is that a type is too coarse: a salon cancelling on a customer
/// and a customer's own cancellation receipt are the same <see cref="NotificationType"/> and must not get
/// the same answer.</para>
/// </remarks>
public class NotificationSuppressionByEventCodeTests
{
    /// <summary>A recipient who has switched SMS off but left push on.</summary>
    private static UserNotificationPreferences SmsDisabled() =>
        UserNotificationPreferences.Create(
            UserId.From(Guid.NewGuid()),
            NotificationPreference.Create(
                NotificationChannel.PushNotification | NotificationChannel.InApp,
                NotificationPreferenceCategory.All));

    // ── The catalogue decides, when the code is known ──

    [Fact]
    public void A_critical_notification_is_sent_on_a_channel_the_recipient_disabled()
    {
        var sent = NotificationSuppressionPolicy.ShouldSend(
            SmsDisabled(),
            NotificationChannel.SMS,
            NotificationType.BookingConfirmation,
            NotificationEventCode.BookingConfirmed);

        Assert.True(sent);
    }

    [Fact]
    public void A_standard_notification_honours_a_disabled_channel()
    {
        var sent = NotificationSuppressionPolicy.ShouldSend(
            SmsDisabled(),
            NotificationChannel.SMS,
            NotificationType.BookingReminder,
            NotificationEventCode.BookingReminder24h);

        Assert.False(sent);
    }

    [Fact]
    public void A_standard_notification_is_sent_on_a_channel_that_is_enabled()
    {
        var sent = NotificationSuppressionPolicy.ShouldSend(
            SmsDisabled(),
            NotificationChannel.PushNotification,
            NotificationType.BookingReminder,
            NotificationEventCode.BookingReminder24h);

        Assert.True(sent);
    }

    [Fact]
    public void Two_notifications_sharing_a_type_can_differ_in_whether_they_can_be_silenced()
    {
        // The case that made the event code necessary. Both are NotificationType.BookingCancelled.
        var preferences = SmsDisabled();

        var salonCancelledOnCustomer = NotificationSuppressionPolicy.ShouldSend(
            preferences,
            NotificationChannel.SMS,
            NotificationType.BookingCancelled,
            NotificationEventCode.BookingCancelledByProvider);

        var customersOwnReceipt = NotificationSuppressionPolicy.ShouldSend(
            preferences,
            NotificationChannel.SMS,
            NotificationType.BookingCancelled,
            NotificationEventCode.BookingCancelledAck);

        Assert.True(salonCancelledOnCustomer);
        Assert.False(customersOwnReceipt);
    }

    [Fact]
    public void A_recipient_with_no_preferences_is_still_notified()
    {
        // Absent preferences must never read as "everything off" — that would mute every notification for
        // every user who has not opened the settings screen.
        var sent = NotificationSuppressionPolicy.ShouldSend(
            preferences: null,
            NotificationChannel.SMS,
            NotificationType.BookingReminder,
            NotificationEventCode.BookingReminder24h);

        Assert.True(sent);
    }

    // ── Characterisation: the old path is untouched ──

    [Fact]
    public void Without_an_event_code_the_previous_behaviour_is_unchanged()
    {
        var preferences = SmsDisabled();

        // Non-suppressible by the legacy set: sent despite SMS being off.
        Assert.True(NotificationSuppressionPolicy.ShouldSend(
            preferences, NotificationChannel.SMS, NotificationType.PaymentReceived));

        // Suppressible by the legacy set: honoured.
        Assert.False(NotificationSuppressionPolicy.ShouldSend(
            preferences, NotificationChannel.SMS, NotificationType.BookingReminder));
    }

    [Fact]
    public void An_unknown_event_code_falls_back_to_the_type()
    {
        // A code with no catalogue entry must not become an accidental override.
        var preferences = SmsDisabled();

        var sent = NotificationSuppressionPolicy.ShouldSend(
            preferences,
            NotificationChannel.SMS,
            NotificationType.PaymentReceived,
            (NotificationEventCode)9999);

        Assert.True(sent);
    }

    [Fact]
    public void The_none_code_falls_back_to_the_type()
    {
        var preferences = SmsDisabled();

        var sent = NotificationSuppressionPolicy.ShouldSend(
            preferences,
            NotificationChannel.SMS,
            NotificationType.BookingReminder,
            NotificationEventCode.None);

        Assert.False(sent);
    }

    [Fact]
    public void The_catalogue_is_at_least_as_protective_as_the_legacy_set()
    {
        // Moving a notification OUT of non-suppressible is the change that needs sign-off. This fails if a
        // catalogue entry would let someone silence something the legacy rules protected.
        var weakened = new List<string>();

        foreach (var code in NotificationEventCatalog.AllCodes)
        {
            var type = NotificationEventCatalog.NotificationTypeFor(code);
            if (!NotificationSuppressionPolicy.NonSuppressible.Contains(type))
                continue;

            if (NotificationEventCatalog.Describe(code).IsSuppressible)
                weakened.Add($"{code} (type {type})");
        }

        Assert.True(
            weakened.Count == 0,
            "These notifications used to be un-silenceable and the catalogue now allows silencing them: " +
            string.Join(", ", weakened));
    }
}

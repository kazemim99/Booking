using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Enums.Extensions;
using Booksy.ServiceCatalog.Domain.Policies;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Xunit;

namespace Booksy.ServiceCatalog.Domain.UnitTests.NotificationAggregate;

/// <summary>
/// C7 notification-delivery-reliability. The preference gate: a customer's channel choices are honoured for
/// ordinary notifications, and ignored for the documented non-suppressible set (money movement and account
/// security). Also covers splitting a combined channel value, since a notification that names three channels
/// must be evaluated against each of them rather than as one opaque number.
/// </summary>
public class NotificationSuppressionPolicyTests
{
    private static UserNotificationPreferences PreferencesWith(NotificationChannel enabledChannels)
    {
        var prefs = UserNotificationPreferences.CreateDefault(UserId.From(Guid.NewGuid()));
        prefs.UpdatePreferences(NotificationPreference.Create(enabledChannels, NotificationType.All));
        return prefs;
    }

    // ---------------------------------------------------------------- Disabled channels are skipped

    [Fact]
    public void A_disabled_channel_is_skipped_for_a_suppressible_notification()
    {
        var prefs = PreferencesWith(NotificationChannel.Email | NotificationChannel.InApp); // SMS off

        Assert.False(NotificationSuppressionPolicy.ShouldSend(
            prefs, NotificationChannel.SMS, NotificationType.BookingReminder));
    }

    [Fact]
    public void An_enabled_channel_is_used_for_a_suppressible_notification()
    {
        var prefs = PreferencesWith(NotificationChannel.Email | NotificationChannel.InApp);

        Assert.True(NotificationSuppressionPolicy.ShouldSend(
            prefs, NotificationChannel.Email, NotificationType.BookingReminder));
    }

    /// <summary>
    /// Most users never open the notification settings screen. If "no preferences row" read as "nothing enabled",
    /// the gate would mute the entire product for them.
    /// </summary>
    [Fact]
    public void A_user_who_never_set_preferences_still_receives_notifications()
    {
        Assert.True(NotificationSuppressionPolicy.ShouldSend(
            null, NotificationChannel.SMS, NotificationType.BookingReminder));
    }

    // ---------------------------------------------------------------- Non-suppressible set

    [Theory]
    [InlineData(NotificationType.PaymentReceived)]
    [InlineData(NotificationType.PaymentFailed)]
    [InlineData(NotificationType.PaymentRefunded)]
    [InlineData(NotificationType.PaymentConfirmed)]
    [InlineData(NotificationType.RefundProcessed)]
    [InlineData(NotificationType.RefundIssued)]
    [InlineData(NotificationType.PayoutCompleted)]
    [InlineData(NotificationType.PayoutProcessed)]
    [InlineData(NotificationType.InvoiceGenerated)]
    [InlineData(NotificationType.SecurityAlert)]
    [InlineData(NotificationType.PasswordReset)]
    [InlineData(NotificationType.PhoneVerification)]
    [InlineData(NotificationType.AccountVerification)]
    public void Financial_and_security_notifications_ignore_disabled_channels(NotificationType type)
    {
        var everythingOff = PreferencesWith(NotificationChannel.None);

        Assert.False(NotificationSuppressionPolicy.IsSuppressible(type));
        Assert.True(NotificationSuppressionPolicy.ShouldSend(everythingOff, NotificationChannel.SMS, type));
        Assert.True(NotificationSuppressionPolicy.ShouldSend(everythingOff, NotificationChannel.Email, type));
    }

    [Theory]
    [InlineData(NotificationType.BookingReminder)]
    [InlineData(NotificationType.BookingConfirmation)]
    [InlineData(NotificationType.Promotions)]
    [InlineData(NotificationType.Newsletter)]
    public void Ordinary_notifications_remain_suppressible(NotificationType type)
    {
        var everythingOff = PreferencesWith(NotificationChannel.None);

        Assert.True(NotificationSuppressionPolicy.IsSuppressible(type));
        Assert.False(NotificationSuppressionPolicy.ShouldSend(everythingOff, NotificationChannel.SMS, type));
    }

    /// <summary>
    /// NotificationType is declared [Flags] but its members from 16777216 up are sequential integers, so
    /// RefundIssued (16777223) "HasFlag" RefundProcessed (16777216). A mask-based non-suppressible set would
    /// therefore leak: unrelated types sharing those bits would silently become unsuppressible.
    /// </summary>
    [Fact]
    public void Set_membership_not_flag_masking_decides_suppressibility()
    {
        // BookingUpdated (16777229) shares bits with several non-suppressible members but is itself ordinary.
        Assert.True(NotificationType.BookingUpdated.HasFlag(NotificationType.RefundProcessed),
            "precondition: the enum's high members are not distinct bits");
        Assert.True(NotificationSuppressionPolicy.IsSuppressible(NotificationType.BookingUpdated));
    }

    // ---------------------------------------------------------------- Channel fan-out

    [Fact]
    public void A_combined_channel_value_enumerates_each_channel_it_names()
    {
        var channels = (NotificationChannel.Email | NotificationChannel.SMS | NotificationChannel.InApp)
            .EnumerateChannels()
            .ToList();

        Assert.Equal(
            new[] { NotificationChannel.Email, NotificationChannel.SMS, NotificationChannel.InApp },
            channels);
    }

    [Fact]
    public void Enumerating_channels_never_yields_None_or_the_composite_All()
    {
        var channels = NotificationChannel.All.EnumerateChannels().ToList();

        Assert.DoesNotContain(NotificationChannel.None, channels);
        Assert.DoesNotContain(NotificationChannel.All, channels);
        Assert.Equal(8, channels.Count); // Email, SMS, Push, InApp, WhatsApp, Telegram, Slack, Phone
    }
}

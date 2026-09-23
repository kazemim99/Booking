using System.Reflection;
using Booksy.ServiceCatalog.Infrastructure.Notifications.Push;
using FirebaseAdmin.Messaging;
using FluentAssertions;

namespace Booksy.ServiceCatalog.Infrastructure.UnitTests.Notifications;

/// <summary>
/// What one push looks like on the wire.
/// </summary>
/// <remarks>
/// <para>Both apps run in production as Flutter WEB on Android Chrome (QA 2026-09-23). A message with only a
/// top-level notification reaches a browser as a left-to-right banner that does nothing when tapped. The web
/// block is what makes it read right-to-left in Persian, carry the app's icon, replace rather than stack a
/// retried copy of itself, and wake a dozing phone.</para>
///
/// <para>The web block rides on EVERY message, not only on tokens registered as web: FCM applies a platform
/// block only when the token is that platform's, so Android and iOS ignore it — and a row registered before
/// platforms were recorded still gets it.</para>
/// </remarks>
public class FcmMessageFactoryTests
{
    private static readonly IReadOnlyDictionary<string, string> Data = new Dictionary<string, string>
    {
        ["notificationId"] = "n-1",
        ["bookingId"] = "b-1",
        ["eventCode"] = "NewBookingRequest",
    };

    [Fact]
    public void A_browser_shows_the_notification_right_to_left_in_Persian()
    {
        var message = FcmMessageFactory.ForDevice("token", "نوبت جدید", "یک مشتری نوبت گرفت", Data);

        message.Webpush.Should().NotBeNull();
        message.Webpush.Notification.Title.Should().Be("نوبت جدید");
        message.Webpush.Notification.Body.Should().Be("یک مشتری نوبت گرفت");
        message.Webpush.Notification.Direction.Should().Be(Direction.RightToLeft);
        message.Webpush.Notification.Language.Should().Be("fa");
    }

    [Fact]
    public void The_banner_carries_the_app_icon_of_whichever_site_registered_the_browser()
    {
        // Relative on purpose: the customer and salon apps are different origins, and the browser resolves the
        // path against the site whose service worker shows the notification.
        var message = FcmMessageFactory.ForDevice("token", "t", "b", Data);

        message.Webpush.Notification.Icon.Should().Be("/icons/Icon-192.png");
    }

    [Fact]
    public void A_retried_notification_replaces_its_earlier_copy_instead_of_stacking()
    {
        // The dispatcher retries on failure; a gateway that took the first attempt after all would otherwise
        // leave two identical banners on the phone.
        var message = FcmMessageFactory.ForDevice("token", "t", "b", Data);

        message.Webpush.Notification.Tag.Should().Be("n-1");
    }

    [Fact]
    public void A_message_without_a_notification_id_is_not_tagged()
    {
        var message = FcmMessageFactory.ForDevice("token", "t", "b", new Dictionary<string, string>());

        message.Webpush.Notification.Tag.Should().BeNull("an empty tag would make every untagged banner replace the last");
    }

    [Fact]
    public void Web_delivery_is_high_urgency_so_a_dozing_phone_still_shows_it()
    {
        var message = FcmMessageFactory.ForDevice("token", "t", "b", Data);

        message.Webpush.Headers.Should().ContainKey("Urgency").WhoseValue.Should().Be("high");
    }

    [Fact]
    public void The_data_a_tap_is_routed_by_is_carried_unchanged()
    {
        var message = FcmMessageFactory.ForDevice("token", "t", "b", Data);

        message.Token.Should().Be("token");
        message.Data.Should().BeEquivalentTo(Data);
        message.Webpush.Data.Should().BeNull("a web data block would REPLACE the top-level data, not add to it");
    }

    [Fact]
    public void Android_and_ios_delivery_is_left_exactly_as_it_was()
    {
        var message = FcmMessageFactory.ForDevice("token", "t", "b", Data);

        message.Notification.Title.Should().Be("t");
        message.Notification.Body.Should().Be("b");
        message.Android.Should().BeNull();
        message.Apns.Should().BeNull();
    }

    [Fact]
    public void A_topic_message_gets_the_same_web_treatment()
    {
        var message = FcmMessageFactory.ForTopic("announcements", "t", "b", Data);

        message.Topic.Should().Be("announcements");
        message.Webpush.Notification.Direction.Should().Be(Direction.RightToLeft);
    }

    [Fact]
    public void Every_message_built_here_is_one_Firebase_accepts()
    {
        // The web block rides on every push, so a block Firebase rejects would stop Android pushes too. Firebase
        // validates before sending; this runs the same validation without a network.
        var validate = typeof(Message).GetMethod("CopyAndValidate", BindingFlags.Instance | BindingFlags.NonPublic);
        validate.Should().NotBeNull("FirebaseAdmin's own pre-send validation is what this test runs");

        foreach (var message in new[]
                 {
                     FcmMessageFactory.ForDevice("token", "t", "b", Data),
                     FcmMessageFactory.ForDevice("token", "t", "b", new Dictionary<string, string>()),
                     FcmMessageFactory.ForTopic("announcements", "t", "b", Data),
                 })
        {
            var act = () => validate!.Invoke(message, null);
            act.Should().NotThrow();
        }
    }
}

using FirebaseAdmin.Messaging;

namespace Booksy.ServiceCatalog.Infrastructure.Notifications.Push
{
    /// <summary>
    /// Builds the FCM message for one push.
    /// </summary>
    /// <remarks>
    /// <para><b>Every message carries a web block.</b> Both apps run in production as Flutter web on Android
    /// Chrome. Without the block a browser shows a left-to-right banner with no icon that does nothing when
    /// tapped. FCM applies a platform block only when the token belongs to that platform, so Android and iOS
    /// ignore it; attaching it everywhere needs no knowledge of which app registered the token, and also covers
    /// rows registered before the platform was recorded.</para>
    ///
    /// <para><b>No click link.</b> A web link must be an absolute https URL, and the backend does not know which
    /// of the two sites (customer or salon) registered a token. The apps' service worker routes a tap itself from
    /// <c>data</c> (<c>bookingId</c>, <c>notificationId</c>), against its own site, using the same route mapping
    /// the apps use for a tapped push on Android.</para>
    ///
    /// <para>Pure, so its output can be checked against Firebase's own validation without a network.</para>
    /// </remarks>
    public static class FcmMessageFactory
    {
        /// <summary>Served by both web apps (web/icons/Icon-192.png); resolved against the site showing it.</summary>
        public const string WebIcon = "/icons/Icon-192.png";

        public static Message ForDevice(
            string deviceToken,
            string title,
            string body,
            IReadOnlyDictionary<string, string> data) =>
            new()
            {
                Token = deviceToken,
                Notification = new Notification { Title = title, Body = body },
                Data = Copy(data),
                Webpush = Web(title, body, data),
            };

        public static Message ForTopic(
            string topic,
            string title,
            string body,
            IReadOnlyDictionary<string, string> data) =>
            new()
            {
                Topic = topic,
                Notification = new Notification { Title = title, Body = body },
                Data = Copy(data),
                Webpush = Web(title, body, data),
            };

        private static WebpushConfig Web(string title, string body, IReadOnlyDictionary<string, string> data) =>
            new()
            {
                // Web Push "Urgency": high lets the push service wake a phone in power saving instead of holding the
                // message until the next time the device is awake.
                Headers = new Dictionary<string, string> { ["Urgency"] = "high" },
                Notification = new WebpushNotification
                {
                    Title = title,
                    Body = body,
                    Icon = WebIcon,
                    Direction = Direction.RightToLeft,
                    Language = "fa",

                    // A retried notification replaces its earlier banner instead of stacking a copy of it.
                    Tag = data.TryGetValue("notificationId", out var id) && !string.IsNullOrWhiteSpace(id) ? id : null,
                },

                // Deliberately no web Data: it would REPLACE the top-level data, which is what a tap is routed by.
            };

        private static Dictionary<string, string> Copy(IReadOnlyDictionary<string, string> data) =>
            data.ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}

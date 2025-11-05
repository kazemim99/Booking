// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/NotificationPreference.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    /// <summary>
    /// Represents user preferences for notification channels and types
    /// </summary>
    public sealed class NotificationPreference : ValueObject
    {
        public NotificationChannel EnabledChannels { get; }
        public NotificationType EnabledTypes { get; }
        public TimeOnly? QuietHoursStart { get; }
        public TimeOnly? QuietHoursEnd { get; }
        public string? PreferredLanguage { get; }
        public bool MarketingOptIn { get; }
        public int MaxNotificationsPerDay { get; }

        private NotificationPreference(
            NotificationChannel enabledChannels,
            NotificationType enabledTypes,
            TimeOnly? quietHoursStart,
            TimeOnly? quietHoursEnd,
            string? preferredLanguage,
            bool marketingOptIn,
            int maxNotificationsPerDay)
        {
            if (maxNotificationsPerDay < 0 || maxNotificationsPerDay > 1000)
                throw new ArgumentException("Max notifications per day must be between 0 and 1000", nameof(maxNotificationsPerDay));

            if (quietHoursStart.HasValue && quietHoursEnd.HasValue)
            {
                if (quietHoursStart.Value == quietHoursEnd.Value)
                    throw new ArgumentException("Quiet hours start and end cannot be the same");
            }

            EnabledChannels = enabledChannels;
            EnabledTypes = enabledTypes;
            QuietHoursStart = quietHoursStart;
            QuietHoursEnd = quietHoursEnd;
            PreferredLanguage = preferredLanguage ?? "en";
            MarketingOptIn = marketingOptIn;
            MaxNotificationsPerDay = maxNotificationsPerDay;
        }

        public static NotificationPreference Create(
            NotificationChannel enabledChannels,
            NotificationType enabledTypes,
            TimeOnly? quietHoursStart = null,
            TimeOnly? quietHoursEnd = null,
            string? preferredLanguage = null,
            bool marketingOptIn = false,
            int maxNotificationsPerDay = 50)
        {
            return new NotificationPreference(
                enabledChannels,
                enabledTypes,
                quietHoursStart,
                quietHoursEnd,
                preferredLanguage,
                marketingOptIn,
                maxNotificationsPerDay);
        }

        /// <summary>
        /// Default preferences for new users
        /// </summary>
        public static NotificationPreference Default => new(
            NotificationChannel.Email | NotificationChannel.SMS | NotificationChannel.InApp,
            NotificationType.All,
            null,
            null,
            "en",
            false,
            50);

        /// <summary>
        /// Minimal notifications (only urgent)
        /// </summary>
        public static NotificationPreference Minimal => new(
            NotificationChannel.Email | NotificationChannel.SMS,
            NotificationType.BookingConfirmation | NotificationType.BookingReminder | NotificationType.PaymentReceived,
            null,
            null,
            "en",
            false,
            10);

        public bool IsChannelEnabled(NotificationChannel channel)
        {
            return EnabledChannels.HasFlag(channel);
        }

        public bool IsTypeEnabled(NotificationType type)
        {
            return EnabledTypes.HasFlag(type);
        }

        public bool IsInQuietHours(DateTime time)
        {
            if (!QuietHoursStart.HasValue || !QuietHoursEnd.HasValue)
                return false;

            var currentTime = TimeOnly.FromDateTime(time);

            // Handle quiet hours that span midnight
            if (QuietHoursStart.Value > QuietHoursEnd.Value)
            {
                return currentTime >= QuietHoursStart.Value || currentTime <= QuietHoursEnd.Value;
            }

            return currentTime >= QuietHoursStart.Value && currentTime <= QuietHoursEnd.Value;
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return EnabledChannels;
            yield return EnabledTypes;
            yield return QuietHoursStart ?? TimeOnly.MinValue;
            yield return QuietHoursEnd ?? TimeOnly.MinValue;
            yield return PreferredLanguage ?? string.Empty;
            yield return MarketingOptIn;
            yield return MaxNotificationsPerDay;
        }
    }
}

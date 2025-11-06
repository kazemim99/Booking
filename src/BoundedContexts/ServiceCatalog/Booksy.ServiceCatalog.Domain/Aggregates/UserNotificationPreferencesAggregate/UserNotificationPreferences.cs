// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/UserNotificationPreferencesAggregate/UserNotificationPreferences.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate
{
    /// <summary>
    /// Aggregate root for managing user notification preferences
    /// </summary>
    public sealed class UserNotificationPreferences : AggregateRoot<UserId>
    {
        public NotificationPreference Preferences { get; private set; }
        public DateTime LastUpdated { get; private set; }
        public DateTime CreatedAt { get; private set; }

        // Private constructor for EF Core
        private UserNotificationPreferences() : base()
        {
            Preferences = NotificationPreference.Default;
        }

        private UserNotificationPreferences(
            UserId userId,
            NotificationPreference preferences) : base()
        {
            Id = userId;
            Preferences = preferences;
            CreatedAt = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Create default notification preferences for a new user
        /// </summary>
        public static UserNotificationPreferences CreateDefault(UserId userId)
        {
            return new UserNotificationPreferences(
                userId,
                NotificationPreference.Default);
        }

        /// <summary>
        /// Create notification preferences with custom settings
        /// </summary>
        public static UserNotificationPreferences Create(
            UserId userId,
            NotificationPreference preferences)
        {
            if (preferences == null)
                throw new ArgumentNullException(nameof(preferences));

            return new UserNotificationPreferences(userId, preferences);
        }

        /// <summary>
        /// Update notification preferences
        /// </summary>
        public void UpdatePreferences(NotificationPreference newPreferences)
        {
            if (newPreferences == null)
                throw new ArgumentNullException(nameof(newPreferences));

            Preferences = newPreferences;
            LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Enable specific notification channels
        /// </summary>
        public void EnableChannels(NotificationChannel channels)
        {
            var newPreferences = NotificationPreference.Create(
                Preferences.EnabledChannels | channels,
                Preferences.EnabledTypes,
                Preferences.QuietHoursStart,
                Preferences.QuietHoursEnd,
                Preferences.PreferredLanguage,
                Preferences.MarketingOptIn,
                Preferences.MaxNotificationsPerDay);

            UpdatePreferences(newPreferences);
        }

        /// <summary>
        /// Disable specific notification channels
        /// </summary>
        public void DisableChannels(NotificationChannel channels)
        {
            var newPreferences = NotificationPreference.Create(
                Preferences.EnabledChannels & ~channels,
                Preferences.EnabledTypes,
                Preferences.QuietHoursStart,
                Preferences.QuietHoursEnd,
                Preferences.PreferredLanguage,
                Preferences.MarketingOptIn,
                Preferences.MaxNotificationsPerDay);

            UpdatePreferences(newPreferences);
        }

        /// <summary>
        /// Enable specific notification types
        /// </summary>
        public void EnableTypes(NotificationType types)
        {
            var newPreferences = NotificationPreference.Create(
                Preferences.EnabledChannels,
                Preferences.EnabledTypes | types,
                Preferences.QuietHoursStart,
                Preferences.QuietHoursEnd,
                Preferences.PreferredLanguage,
                Preferences.MarketingOptIn,
                Preferences.MaxNotificationsPerDay);

            UpdatePreferences(newPreferences);
        }

        /// <summary>
        /// Disable specific notification types
        /// </summary>
        public void DisableTypes(NotificationType types)
        {
            var newPreferences = NotificationPreference.Create(
                Preferences.EnabledChannels,
                Preferences.EnabledTypes & ~types,
                Preferences.QuietHoursStart,
                Preferences.QuietHoursEnd,
                Preferences.PreferredLanguage,
                Preferences.MarketingOptIn,
                Preferences.MaxNotificationsPerDay);

            UpdatePreferences(newPreferences);
        }

        /// <summary>
        /// Set quiet hours for notifications
        /// </summary>
        public void SetQuietHours(TimeOnly? start, TimeOnly? end)
        {
            var newPreferences = NotificationPreference.Create(
                Preferences.EnabledChannels,
                Preferences.EnabledTypes,
                start,
                end,
                Preferences.PreferredLanguage,
                Preferences.MarketingOptIn,
                Preferences.MaxNotificationsPerDay);

            UpdatePreferences(newPreferences);
        }

        /// <summary>
        /// Clear quiet hours
        /// </summary>
        public void ClearQuietHours()
        {
            SetQuietHours(null, null);
        }

        /// <summary>
        /// Update preferred language
        /// </summary>
        public void SetPreferredLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                throw new ArgumentException("Language code cannot be empty", nameof(languageCode));

            var newPreferences = NotificationPreference.Create(
                Preferences.EnabledChannels,
                Preferences.EnabledTypes,
                Preferences.QuietHoursStart,
                Preferences.QuietHoursEnd,
                languageCode,
                Preferences.MarketingOptIn,
                Preferences.MaxNotificationsPerDay);

            UpdatePreferences(newPreferences);
        }

        /// <summary>
        /// Update marketing opt-in status
        /// </summary>
        public void SetMarketingOptIn(bool optIn)
        {
            var newPreferences = NotificationPreference.Create(
                Preferences.EnabledChannels,
                Preferences.EnabledTypes,
                Preferences.QuietHoursStart,
                Preferences.QuietHoursEnd,
                Preferences.PreferredLanguage,
                optIn,
                Preferences.MaxNotificationsPerDay);

            UpdatePreferences(newPreferences);
        }

        /// <summary>
        /// Set maximum notifications per day limit
        /// </summary>
        public void SetMaxNotificationsPerDay(int maxNotifications)
        {
            if (maxNotifications < 0 || maxNotifications > 1000)
                throw new ArgumentException("Max notifications per day must be between 0 and 1000", nameof(maxNotifications));

            var newPreferences = NotificationPreference.Create(
                Preferences.EnabledChannels,
                Preferences.EnabledTypes,
                Preferences.QuietHoursStart,
                Preferences.QuietHoursEnd,
                Preferences.PreferredLanguage,
                Preferences.MarketingOptIn,
                maxNotifications);

            UpdatePreferences(newPreferences);
        }

        /// <summary>
        /// Reset to default preferences
        /// </summary>
        public void ResetToDefaults()
        {
            UpdatePreferences(NotificationPreference.Default);
        }

        /// <summary>
        /// Set to minimal notifications only
        /// </summary>
        public void SetMinimalNotifications()
        {
            UpdatePreferences(NotificationPreference.Minimal);
        }

        /// <summary>
        /// Check if a notification should be sent based on preferences
        /// </summary>
        public bool ShouldSendNotification(NotificationChannel channel, NotificationType type, DateTime time)
        {
            // Check if channel is enabled
            if (!Preferences.IsChannelEnabled(channel))
                return false;

            // Check if type is enabled
            if (!Preferences.IsTypeEnabled(type))
                return false;

            // Check quiet hours
            if (Preferences.IsInQuietHours(time))
                return false;

            return true;
        }
    }
}

// ========================================
// Booksy.UserManagement.Domain/ValueObjects/NotificationPreferences.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.UserManagement.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing customer notification preferences
    /// </summary>
    public sealed class NotificationPreferences : ValueObject
    {
        public bool SmsEnabled { get; private set; }
        public bool EmailEnabled { get; private set; }
        public string ReminderTiming { get; private set; }

        // Valid reminder timing options
        public static readonly string[] ValidTimings = new[] { "1h", "24h", "3d" };

        private NotificationPreferences()
        {
            ReminderTiming = "24h"; // Default
        }

        public static NotificationPreferences Create(
            bool smsEnabled = true,
            bool emailEnabled = true,
            string reminderTiming = "24h")
        {
            if (!ValidTimings.Contains(reminderTiming))
                throw new ArgumentException($"Invalid reminder timing. Must be one of: {string.Join(", ", ValidTimings)}", nameof(reminderTiming));

            return new NotificationPreferences
            {
                SmsEnabled = smsEnabled,
                EmailEnabled = emailEnabled,
                ReminderTiming = reminderTiming
            };
        }

        public static NotificationPreferences Default()
        {
            return new NotificationPreferences
            {
                SmsEnabled = true,
                EmailEnabled = true,
                ReminderTiming = "24h"
            };
        }

        public NotificationPreferences UpdateSms(bool enabled)
        {
            return new NotificationPreferences
            {
                SmsEnabled = enabled,
                EmailEnabled = this.EmailEnabled,
                ReminderTiming = this.ReminderTiming
            };
        }

        public NotificationPreferences UpdateEmail(bool enabled)
        {
            return new NotificationPreferences
            {
                SmsEnabled = this.SmsEnabled,
                EmailEnabled = enabled,
                ReminderTiming = this.ReminderTiming
            };
        }

        public NotificationPreferences UpdateReminderTiming(string timing)
        {
            if (!ValidTimings.Contains(timing))
                throw new ArgumentException($"Invalid reminder timing. Must be one of: {string.Join(", ", ValidTimings)}", nameof(timing));

            return new NotificationPreferences
            {
                SmsEnabled = this.SmsEnabled,
                EmailEnabled = this.EmailEnabled,
                ReminderTiming = timing
            };
        }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return SmsEnabled;
            yield return EmailEnabled;
            yield return ReminderTiming;
        }
    }
}

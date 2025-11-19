// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/UpdateNotificationPreferences/UpdateNotificationPreferencesResult.cs
// ========================================

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.UpdateNotificationPreferences
{
    /// <summary>
    /// Result of updating customer notification preferences
    /// </summary>
    public sealed record UpdateNotificationPreferencesResult
    {
        public Guid CustomerId { get; init; }
        public bool SmsEnabled { get; init; }
        public bool EmailEnabled { get; init; }
        public string ReminderTiming { get; init; } = string.Empty;
        public DateTime UpdatedAt { get; init; }

        public static UpdateNotificationPreferencesResult Success(
            Guid customerId,
            bool smsEnabled,
            bool emailEnabled,
            string reminderTiming,
            DateTime updatedAt)
        {
            return new UpdateNotificationPreferencesResult
            {
                CustomerId = customerId,
                SmsEnabled = smsEnabled,
                EmailEnabled = emailEnabled,
                ReminderTiming = reminderTiming,
                UpdatedAt = updatedAt
            };
        }
    }
}

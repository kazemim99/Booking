namespace Booksy.ServiceCatalog.Domain.Enums.Extensions;

/// <summary>
/// Extension methods for notification enums
/// </summary>
public static class NotificationExtensions
{
    /// <summary>
    /// Get the TimeSpan equivalent for notification timing
    /// </summary>
    public static TimeSpan? GetTimeSpan(this NotificationTiming timing)
    {
        return timing switch
        {
            NotificationTiming.Immediate => TimeSpan.Zero,
            NotificationTiming.FiveMinutesBefore => TimeSpan.FromMinutes(5),
            NotificationTiming.FifteenMinutesBefore => TimeSpan.FromMinutes(15),
            NotificationTiming.ThirtyMinutesBefore => TimeSpan.FromMinutes(30),
            NotificationTiming.OneHourBefore => TimeSpan.FromHours(1),
            NotificationTiming.TwoHoursBefore => TimeSpan.FromHours(2),
            NotificationTiming.OneDayBefore => TimeSpan.FromDays(1),
            NotificationTiming.TwoDaysBefore => TimeSpan.FromDays(2),
            NotificationTiming.Never => null,
            _ => null
        };
    }

    /// <summary>
    /// Check if a channel supports rich content (images, formatting, etc.)
    /// </summary>
    public static bool SupportsRichContent(this NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.Email => true,
            NotificationChannel.PushNotification => true,
            NotificationChannel.InApp => true,
            NotificationChannel.WhatsApp => true,
            NotificationChannel.Telegram => true,
            NotificationChannel.Slack => true,
            _ => false
        };
    }

    /// <summary>
    /// Check if a channel supports immediate delivery
    /// </summary>
    public static bool SupportsImmediateDelivery(this NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.SMS => true,
            NotificationChannel.PushNotification => true,
            NotificationChannel.InApp => true,
            NotificationChannel.WhatsApp => true,
            NotificationChannel.Telegram => true,
            NotificationChannel.Slack => true,
            NotificationChannel.Phone => true,
            _ => false
        };
    }

    /// <summary>
    /// Get the default priority for a notification type
    /// </summary>
    public static NotificationPriority GetDefaultPriority(this NotificationType type)
    {
        return type switch
        {
            NotificationType.SecurityAlert => NotificationPriority.Critical,
            NotificationType.PaymentFailed => NotificationPriority.Critical,
            NotificationType.ScheduleConflict => NotificationPriority.Urgent,
            NotificationType.ClientNoShow => NotificationPriority.Urgent,
            NotificationType.BookingCancelled => NotificationPriority.High,
            NotificationType.NewBooking => NotificationPriority.High,
            NotificationType.BookingRescheduled => NotificationPriority.High,
            NotificationType.PaymentReceived => NotificationPriority.Normal,
            NotificationType.BookingReminder => NotificationPriority.Normal,
            NotificationType.NewReview => NotificationPriority.Normal,
            NotificationType.StaffUnavailable => NotificationPriority.Normal,
            NotificationType.SystemMaintenance => NotificationPriority.Low,
            NotificationType.Promotions => NotificationPriority.Low,
            NotificationType.Newsletter => NotificationPriority.Low,
            _ => NotificationPriority.Normal
        };
    }

    /// <summary>
    /// Get recommended channels for a notification type
    /// </summary>
    public static NotificationChannel GetRecommendedChannels(this NotificationType type)
    {
        return type switch
        {
            NotificationType.SecurityAlert => NotificationChannel.Email | NotificationChannel.SMS | NotificationChannel.PushNotification,
            NotificationType.PaymentFailed => NotificationChannel.Email | NotificationChannel.SMS,
            NotificationType.BookingReminder => NotificationChannel.SMS | NotificationChannel.PushNotification,
            NotificationType.NewBooking => NotificationChannel.Email | NotificationChannel.PushNotification | NotificationChannel.InApp,
            NotificationType.BookingCancelled => NotificationChannel.Email | NotificationChannel.SMS | NotificationChannel.PushNotification,
            NotificationType.ScheduleConflict => NotificationChannel.Email | NotificationChannel.InApp,
            NotificationType.PaymentReceived => NotificationChannel.Email | NotificationChannel.InApp,
            NotificationType.NewReview => NotificationChannel.Email | NotificationChannel.InApp,
            NotificationType.SystemMaintenance => NotificationChannel.Email | NotificationChannel.InApp,
            NotificationType.Promotions => NotificationChannel.Email | NotificationChannel.PushNotification,
            NotificationType.Newsletter => NotificationChannel.Email,
            _ => NotificationChannel.Email | NotificationChannel.InApp
        };
    }

    /// <summary>
    /// Check if notification type is time-sensitive
    /// </summary>
    public static bool IsTimeSensitive(this NotificationType type)
    {
        return type switch
        {
            NotificationType.BookingReminder => true,
            NotificationType.SecurityAlert => true,
            NotificationType.PaymentFailed => true,
            NotificationType.ScheduleConflict => true,
            NotificationType.ClientNoShow => true,
            NotificationType.ClientLateArrival => true,
            NotificationType.StaffUnavailable => true,
            _ => false
        };
    }

    /// <summary>
    /// Get user-friendly display name for channel
    /// </summary>
    public static string GetDisplayName(this NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.Email => "Email",
            NotificationChannel.SMS => "Text Message",
            NotificationChannel.PushNotification => "Push Notification",
            NotificationChannel.InApp => "In-App Notification",
            NotificationChannel.WhatsApp => "WhatsApp",
            NotificationChannel.Telegram => "Telegram",
            NotificationChannel.Slack => "Slack",
            NotificationChannel.Phone => "Phone Call",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get display name for notification timing
    /// </summary>
    public static string GetDisplayName(this NotificationTiming timing)
    {
        return timing switch
        {
            NotificationTiming.Immediate => "Immediately",
            NotificationTiming.FiveMinutesBefore => "5 minutes before",
            NotificationTiming.FifteenMinutesBefore => "15 minutes before",
            NotificationTiming.ThirtyMinutesBefore => "30 minutes before",
            NotificationTiming.OneHourBefore => "1 hour before",
            NotificationTiming.TwoHoursBefore => "2 hours before",
            NotificationTiming.OneDayBefore => "1 day before",
            NotificationTiming.TwoDaysBefore => "2 days before",
            NotificationTiming.Weekly => "Weekly summary",
            NotificationTiming.Daily => "Daily summary",
            NotificationTiming.Never => "Never",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Check if timing is suitable for a notification type
    /// </summary>
    public static bool IsSuitableFor(this NotificationTiming timing, NotificationType type)
    {
        return type switch
        {
            NotificationType.BookingReminder => timing != NotificationTiming.Never && timing != NotificationTiming.Weekly && timing != NotificationTiming.Daily,
            NotificationType.SecurityAlert => timing == NotificationTiming.Immediate,
            NotificationType.PaymentFailed => timing == NotificationTiming.Immediate,
            NotificationType.Newsletter => timing == NotificationTiming.Weekly || timing == NotificationTiming.Daily,
            NotificationType.BusinessMetrics => timing == NotificationTiming.Weekly || timing == NotificationTiming.Daily,
            _ => true
        };
    }

    /// <summary>
    /// Get days of week as list
    /// </summary>
    public static IEnumerable<DayOfWeek> GetDaysOfWeek(this NotificationDays days)
    {
        var result = new List<DayOfWeek>();

        if (days.HasFlag(NotificationDays.Monday)) result.Add(DayOfWeek.Monday);
        if (days.HasFlag(NotificationDays.Tuesday)) result.Add(DayOfWeek.Tuesday);
        if (days.HasFlag(NotificationDays.Wednesday)) result.Add(DayOfWeek.Wednesday);
        if (days.HasFlag(NotificationDays.Thursday)) result.Add(DayOfWeek.Thursday);
        if (days.HasFlag(NotificationDays.Friday)) result.Add(DayOfWeek.Friday);
        if (days.HasFlag(NotificationDays.Saturday)) result.Add(DayOfWeek.Saturday);
        if (days.HasFlag(NotificationDays.Sunday)) result.Add(DayOfWeek.Sunday);

        return result;
    }

    /// <summary>
    /// Check if notification should be sent today
    /// </summary>
    public static bool ShouldNotifyToday(this NotificationDays days)
    {
        var today = DateTime.Now.DayOfWeek;
        var todayFlag = today switch
        {
            System.DayOfWeek.Monday => NotificationDays.Monday,
            System.DayOfWeek.Tuesday => NotificationDays.Tuesday,
            System.DayOfWeek.Wednesday => NotificationDays.Wednesday,
            System.DayOfWeek.Thursday => NotificationDays.Thursday,
            System.DayOfWeek.Friday => NotificationDays.Friday,
            System.DayOfWeek.Saturday => NotificationDays.Saturday,
            System.DayOfWeek.Sunday => NotificationDays.Sunday,
            _ => NotificationDays.None
        };

        return days.HasFlag(todayFlag);
    }
}
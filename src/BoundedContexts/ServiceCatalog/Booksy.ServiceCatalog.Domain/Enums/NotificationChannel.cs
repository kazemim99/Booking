namespace Booksy.ServiceCatalog.Domain.Enums;

/// <summary>
/// Notification channel preferences for providers and clients
/// </summary>
[Flags]
public enum NotificationChannel
{
    None = 0,
    Email = 1,
    SMS = 2,
    PushNotification = 4,
    InApp = 8,
    WhatsApp = 16,
    Telegram = 32,
    Slack = 64,
    Phone = 128,
    All = Email | SMS | PushNotification | InApp | WhatsApp | Telegram | Slack | Phone
}

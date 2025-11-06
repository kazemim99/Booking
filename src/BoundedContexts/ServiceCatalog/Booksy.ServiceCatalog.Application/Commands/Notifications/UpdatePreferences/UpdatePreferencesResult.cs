// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/UpdatePreferences/UpdatePreferencesResult.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.UpdatePreferences
{
    /// <summary>
    /// Result of updating user notification preferences
    /// </summary>
    public sealed record UpdatePreferencesResult(
        Guid UserId,
        NotificationChannel EnabledChannels,
        NotificationType EnabledTypes,
        TimeOnly? QuietHoursStart,
        TimeOnly? QuietHoursEnd,
        string PreferredLanguage,
        bool MarketingOptIn,
        int MaxNotificationsPerDay,
        DateTime LastUpdated);
}

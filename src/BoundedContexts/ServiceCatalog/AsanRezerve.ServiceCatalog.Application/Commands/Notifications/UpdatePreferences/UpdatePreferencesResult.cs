// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Notifications/UpdatePreferences/UpdatePreferencesResult.cs
// ========================================
using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Notifications.UpdatePreferences
{
    /// <summary>
    /// Result of updating user notification preferences
    /// </summary>
    public sealed record UpdatePreferencesResult(
        Guid UserId,
        NotificationChannel EnabledChannels,
        NotificationPreferenceCategory EnabledTypes,
        TimeOnly? QuietHoursStart,
        TimeOnly? QuietHoursEnd,
        string PreferredLanguage,
        bool MarketingOptIn,
        int MaxNotificationsPerDay,
        DateTime LastUpdated);
}

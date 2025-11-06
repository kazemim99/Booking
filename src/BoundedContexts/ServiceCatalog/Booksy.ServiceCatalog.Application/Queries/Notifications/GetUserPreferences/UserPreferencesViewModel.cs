// ========================================
// Booksy.ServiceCatalog.Application/Queries/Notifications/GetUserPreferences/UserPreferencesViewModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Notifications.GetUserPreferences
{
    /// <summary>
    /// View model for user notification preferences
    /// </summary>
    public sealed record UserPreferencesViewModel(
        Guid UserId,
        List<string> EnabledChannels,
        List<string> EnabledTypes,
        TimeOnly? QuietHoursStart,
        TimeOnly? QuietHoursEnd,
        string PreferredLanguage,
        bool MarketingOptIn,
        int MaxNotificationsPerDay,
        DateTime LastUpdated,
        DateTime CreatedAt);
}

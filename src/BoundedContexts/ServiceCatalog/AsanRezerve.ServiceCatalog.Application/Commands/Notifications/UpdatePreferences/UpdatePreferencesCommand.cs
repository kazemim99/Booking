// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Notifications/UpdatePreferences/UpdatePreferencesCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Notifications.UpdatePreferences
{
    /// <summary>
    /// Command to update user notification preferences
    /// </summary>
    public sealed record UpdatePreferencesCommand(
        Guid UserId,
        NotificationChannel? EnabledChannels = null,
        NotificationPreferenceCategory? EnabledTypes = null,
        TimeOnly? QuietHoursStart = null,
        TimeOnly? QuietHoursEnd = null,
        string? PreferredLanguage = null,
        bool? MarketingOptIn = null,
        int? MaxNotificationsPerDay = null,
        bool ResetToDefaults = false,
        bool SetMinimal = false,
        Guid? IdempotencyKey = null) : ICommand<UpdatePreferencesResult>;
}

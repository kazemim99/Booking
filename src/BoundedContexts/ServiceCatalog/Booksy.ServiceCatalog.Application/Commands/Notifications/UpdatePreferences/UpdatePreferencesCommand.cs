// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/UpdatePreferences/UpdatePreferencesCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.UpdatePreferences
{
    /// <summary>
    /// Command to update user notification preferences
    /// </summary>
    public sealed record UpdatePreferencesCommand(
        Guid UserId,
        NotificationChannel? EnabledChannels = null,
        NotificationType? EnabledTypes = null,
        TimeOnly? QuietHoursStart = null,
        TimeOnly? QuietHoursEnd = null,
        string? PreferredLanguage = null,
        bool? MarketingOptIn = null,
        int? MaxNotificationsPerDay = null,
        bool ResetToDefaults = false,
        bool SetMinimal = false) : ICommand<UpdatePreferencesResult>;
}

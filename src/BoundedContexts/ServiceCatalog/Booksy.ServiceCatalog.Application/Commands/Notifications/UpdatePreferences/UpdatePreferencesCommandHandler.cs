// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/UpdatePreferences/UpdatePreferencesCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.UpdatePreferences
{
    /// <summary>
    /// Handler for UpdatePreferencesCommand
    /// </summary>
    public sealed class UpdatePreferencesCommandHandler
        : ICommandHandler<UpdatePreferencesCommand, UpdatePreferencesResult>
    {
        private readonly IUserNotificationPreferencesRepository _preferencesRepository;
        private readonly ILogger<UpdatePreferencesCommandHandler> _logger;

        public UpdatePreferencesCommandHandler(
            IUserNotificationPreferencesRepository preferencesRepository,
            ILogger<UpdatePreferencesCommandHandler> logger)
        {
            _preferencesRepository = preferencesRepository;
            _logger = logger;
        }

        public async Task<UpdatePreferencesResult> Handle(
            UpdatePreferencesCommand command,
            CancellationToken cancellationToken)
        {
            var userId = UserId.From(command.UserId);

            // Get existing preferences or create default
            var userPreferences = await _preferencesRepository.GetByUserIdAsync(userId, cancellationToken);
            var isNew = userPreferences == null;

            if (userPreferences == null)
            {
                userPreferences = UserNotificationPreferences.CreateDefault(userId);
                _logger.LogInformation(
                    "Creating default notification preferences for user: {UserId}",
                    command.UserId);
            }

            // Handle special operations
            if (command.ResetToDefaults)
            {
                userPreferences.ResetToDefaults();
                _logger.LogInformation(
                    "Reset notification preferences to defaults for user: {UserId}",
                    command.UserId);
            }
            else if (command.SetMinimal)
            {
                userPreferences.SetMinimalNotifications();
                _logger.LogInformation(
                    "Set minimal notification preferences for user: {UserId}",
                    command.UserId);
            }
            else
            {
                // Update individual settings
                if (command.EnabledChannels.HasValue || command.EnabledTypes.HasValue ||
                    command.QuietHoursStart.HasValue || command.QuietHoursEnd.HasValue ||
                    command.PreferredLanguage != null || command.MarketingOptIn.HasValue ||
                    command.MaxNotificationsPerDay.HasValue)
                {
                    var newPreferences = NotificationPreference.Create(
                        command.EnabledChannels ?? userPreferences.Preferences.EnabledChannels,
                        command.EnabledTypes ?? userPreferences.Preferences.EnabledTypes,
                        command.QuietHoursStart ?? userPreferences.Preferences.QuietHoursStart,
                        command.QuietHoursEnd ?? userPreferences.Preferences.QuietHoursEnd,
                        command.PreferredLanguage ?? userPreferences.Preferences.PreferredLanguage,
                        command.MarketingOptIn ?? userPreferences.Preferences.MarketingOptIn,
                        command.MaxNotificationsPerDay ?? userPreferences.Preferences.MaxNotificationsPerDay);

                    userPreferences.UpdatePreferences(newPreferences);
                }
            }

            // Save or update
            if (isNew)
            {
                await _preferencesRepository.SaveAsync(userPreferences, cancellationToken);
            }
            else
            {
                await _preferencesRepository.UpdateAsync(userPreferences, cancellationToken);
            }

            _logger.LogInformation(
                "Updated notification preferences for user: {UserId}, Channels={Channels}, Types={Types}",
                command.UserId,
                userPreferences.Preferences.EnabledChannels,
                userPreferences.Preferences.EnabledTypes);

            return new UpdatePreferencesResult(
                command.UserId,
                userPreferences.Preferences.EnabledChannels,
                userPreferences.Preferences.EnabledTypes,
                userPreferences.Preferences.QuietHoursStart,
                userPreferences.Preferences.QuietHoursEnd,
                userPreferences.Preferences.PreferredLanguage ?? "en",
                userPreferences.Preferences.MarketingOptIn,
                userPreferences.Preferences.MaxNotificationsPerDay,
                userPreferences.LastUpdated);
        }
    }
}

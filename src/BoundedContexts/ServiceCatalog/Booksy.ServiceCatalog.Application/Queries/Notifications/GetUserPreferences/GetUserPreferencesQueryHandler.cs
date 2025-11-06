// ========================================
// Booksy.ServiceCatalog.Application/Queries/Notifications/GetUserPreferences/GetUserPreferencesQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Notifications.GetUserPreferences
{
    /// <summary>
    /// Handler for GetUserPreferencesQuery
    /// </summary>
    public sealed class GetUserPreferencesQueryHandler
        : IQueryHandler<GetUserPreferencesQuery, UserPreferencesViewModel?>
    {
        private readonly IUserNotificationPreferencesRepository _preferencesRepository;
        private readonly ILogger<GetUserPreferencesQueryHandler> _logger;

        public GetUserPreferencesQueryHandler(
            IUserNotificationPreferencesRepository preferencesRepository,
            ILogger<GetUserPreferencesQueryHandler> logger)
        {
            _preferencesRepository = preferencesRepository;
            _logger = logger;
        }

        public async Task<UserPreferencesViewModel?> Handle(
            GetUserPreferencesQuery request,
            CancellationToken cancellationToken)
        {
            var userId = UserId.From(request.UserId);
            var preferences = await _preferencesRepository.GetByUserIdAsync(userId, cancellationToken);

            if (preferences == null)
            {
                _logger.LogInformation("No custom preferences found for user {UserId}, returning null", request.UserId);
                return null;
            }

            var enabledChannels = preferences.Preferences.EnabledChannels.ToString()
                .Split(',')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();

            var enabledTypes = preferences.Preferences.EnabledTypes.ToString()
                .Split(',')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            return new UserPreferencesViewModel(
                request.UserId,
                enabledChannels,
                enabledTypes,
                preferences.Preferences.QuietHoursStart,
                preferences.Preferences.QuietHoursEnd,
                preferences.Preferences.PreferredLanguage ?? "en",
                preferences.Preferences.MarketingOptIn,
                preferences.Preferences.MaxNotificationsPerDay,
                preferences.LastUpdated,
                preferences.CreatedAt);
        }
    }
}

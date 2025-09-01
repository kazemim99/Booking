

// Booksy.UserManagement.Infrastructure/Services/Application/UserPreferencesService.cs
using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Infrastructure.Services.Application
{
    public class UserPreferencesService : IUserPreferencesService
    {
        private readonly IUserReadRepository _userReadRepository;
        private readonly IUserWriteRepository _userWriteRepository;

        public UserPreferencesService(
            IUserReadRepository userReadRepository,
            IUserWriteRepository userWriteRepository)
        {
            _userReadRepository = userReadRepository;
            _userWriteRepository = userWriteRepository;
        }

        public async Task<Dictionary<string, string>> GetPreferencesAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _userReadRepository.GetByIdAsync(userId, cancellationToken);
            return user?.Profile?.Preferences ?? new Dictionary<string, string>();
        }

        public async Task SetPreferenceAsync(
            UserId userId,
            string key,
            string value,
            CancellationToken cancellationToken = default)
        {
            var user = await _userReadRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                throw new InvalidOperationException($"User {userId} not found");

            user.Profile.SetPreference(key, value);
            await _userWriteRepository.UpdateAsync(user, cancellationToken);
        }

        public async Task SetPreferencesAsync(
            UserId userId,
            Dictionary<string, string> preferences,
            CancellationToken cancellationToken = default)
        {
            var user = await _userReadRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                throw new InvalidOperationException($"User {userId} not found");

            foreach (var preference in preferences)
            {
                user.Profile.SetPreference(preference.Key, preference.Value);
            }

            await _userWriteRepository.UpdateAsync(user, cancellationToken);
        }

        public async Task RemovePreferenceAsync(
            UserId userId,
            string key,
            CancellationToken cancellationToken = default)
        {
            var user = await _userReadRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                throw new InvalidOperationException($"User {userId} not found");

            if (user.Profile.Preferences.ContainsKey(key))
            {
                user.Profile.Preferences.Remove(key);
                await _userWriteRepository.UpdateAsync(user, cancellationToken);
            }
        }

        public async Task ClearPreferencesAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _userReadRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                throw new InvalidOperationException($"User {userId} not found");

            user.Profile.Preferences.Clear();
            await _userWriteRepository.UpdateAsync(user, cancellationToken);
        }
    }
}
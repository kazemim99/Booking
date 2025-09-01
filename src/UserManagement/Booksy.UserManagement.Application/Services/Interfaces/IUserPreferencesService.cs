// ========================================
// Event Handlers - Domain Events
// ========================================

// Booksy.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Application.Services.Interfaces
{
    public interface IUserPreferencesService
    {
        Task<Dictionary<string, string>> GetPreferencesAsync(UserId userId, CancellationToken cancellationToken = default);
        Task SetPreferenceAsync(UserId userId, string key, string value, CancellationToken cancellationToken = default);
        Task SetPreferencesAsync(UserId userId, Dictionary<string, string> preferences, CancellationToken cancellationToken = default);
        Task RemovePreferenceAsync(UserId userId, string key, CancellationToken cancellationToken = default);
        Task ClearPreferencesAsync(UserId userId, CancellationToken cancellationToken = default);
    }
}

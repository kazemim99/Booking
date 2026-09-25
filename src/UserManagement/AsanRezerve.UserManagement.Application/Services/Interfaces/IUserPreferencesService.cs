// ========================================
// Event Handlers - Domain Events
// ========================================

// AsanRezerve.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Application.Services.Interfaces
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

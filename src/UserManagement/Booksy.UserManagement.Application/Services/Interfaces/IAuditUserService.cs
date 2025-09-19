// ========================================
// Event Handlers - Domain Events
// ========================================

// Booksy.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Application.Services.Interfaces
{
    public interface IAuditUserService
    {
        Task LogRegistrationAsync(UserId userId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
        Task LogActivationAsync(UserId userId, CancellationToken cancellationToken = default);
        Task LogSuccessfulLoginAsync(UserId userId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
        Task LogFailedLoginAsync(string email, string reason, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
        Task LogPasswordChangeAsync(UserId userId, CancellationToken cancellationToken = default);
        Task LogPasswordResetRequestAsync(UserId userId, string? ipAddress, CancellationToken cancellationToken = default);
        Task LogProfileUpdateAsync(UserId userId, Dictionary<string, object> changes, CancellationToken cancellationToken = default);
        Task LogAccountDeactivationAsync(UserId userId, string reason, CancellationToken cancellationToken = default);
    }
}

// Booksy.UserManagement.Application/Services/Interfaces/IUserPreferencesService.cs

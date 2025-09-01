// ========================================
// Event Handlers - Domain Events
// ========================================

// Booksy.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
using Booksy.UserManagement.Application.CQRS.Commands.RegisterUser;

namespace Booksy.UserManagement.Application.Services.Interfaces
{
    public interface IUserRegistrationService
    {
        Task<bool> ValidateRegistrationAsync(RegisterUserCommand command, CancellationToken cancellationToken = default);
        Task<bool> IsEmailBlacklistedAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> IsIpAddressBlockedAsync(string? ipAddress, CancellationToken cancellationToken = default);
        Task<int> GetRegistrationAttemptsAsync(string email, TimeSpan window, CancellationToken cancellationToken = default);
    }
}

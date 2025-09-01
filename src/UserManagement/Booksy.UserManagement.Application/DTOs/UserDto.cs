// ========================================
// Event Handlers - Domain Events
// ========================================

// Booksy.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Application.DTOs
{
    public sealed class UserDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public UserStatus Status { get; init; }
        public UserType Type { get; init; }
        public List<string> Roles { get; init; } = new();
        public DateTime RegisteredAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
    }
}

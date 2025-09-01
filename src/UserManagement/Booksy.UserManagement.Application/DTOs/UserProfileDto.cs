// ========================================
// Event Handlers - Domain Events
// ========================================

// Booksy.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
// Booksy.UserManagement.Application/DTOs/UserProfileDto.cs
namespace Booksy.UserManagement.Application.DTOs
{
    public sealed class UserProfileDto
    {
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public string? Gender { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Bio { get; init; }
        public string? AvatarUrl { get; init; }
        public string? PreferredLanguage { get; init; }
        public string? TimeZone { get; init; }
        public AddressDto? Address { get; init; }
        public Dictionary<string, string> Preferences { get; init; } = new();
    }
}

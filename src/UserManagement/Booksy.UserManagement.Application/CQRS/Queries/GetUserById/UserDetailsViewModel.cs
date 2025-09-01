// ========================================
// Booksy.UserManagement.Application/Queries/GetUserById/GetUserByIdQuery.cs
// ========================================

namespace Booksy.UserManagement.Application.CQRS.Queries.GetUserById
{
    public sealed class UserDetailsViewModel
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public DateTime? DateOfBirth { get; init; }
        public int? Age { get; init; }
        public string? Gender { get; init; }
        public string? PhoneNumber { get; init; }
        public string? AlternatePhoneNumber { get; init; }
        public AddressViewModel? Address { get; init; }
        public string? AvatarUrl { get; init; }
        public string? Bio { get; init; }
        public string? PreferredLanguage { get; init; }
        public string? TimeZone { get; init; }
        public string Status { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public List<RoleViewModel> Roles { get; init; } = new();
        public bool TwoFactorEnabled { get; init; }
        public bool IsLocked { get; init; }
        public DateTime? LockedUntil { get; init; }
        public DateTime RegisteredAt { get; init; }
        public DateTime? ActivatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public DateTime? LastPasswordChangeAt { get; init; }
        public int ActiveSessions { get; init; }
        public Dictionary<string, string> Preferences { get; init; } = new();
    }
}



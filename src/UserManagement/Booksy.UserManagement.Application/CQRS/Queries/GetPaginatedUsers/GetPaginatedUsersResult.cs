// ========================================
// Booksy.UserManagement.Application/Queries/GetUserById/GetUserByIdQuery.cs
// ========================================
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Application.CQRS.Queries.GetPaginatedUsers
{
    public sealed class GetPaginatedUsersResult
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
        public UserStatus Status { get; init; }
        public UserType Type { get; init; }
        public List<string> Roles { get; init; } = new();
        public DateTime RegisteredAt { get; init; }
        public DateTime? ActivatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public bool IsLocked { get; init; }
        public bool TwoFactorEnabled { get; init; }
        public string? AvatarUrl { get; init; }
    }
}



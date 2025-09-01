// ========================================
// Booksy.UserManagement.Application/Queries/GetUserById/GetUserByIdQuery.cs
// ========================================
// ========================================
// Booksy.UserManagement.Application/Queries/GetUsersByStatus/UserListViewModel.cs
// ========================================
namespace Booksy.UserManagement.Application.CQRS.Queries.GetUsersByStatus
{
    public sealed class GetUsersByStatusResult
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public DateTime RegisteredAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public bool IsLocked { get; init; }
        public string? AvatarUrl { get; init; }
    }
}


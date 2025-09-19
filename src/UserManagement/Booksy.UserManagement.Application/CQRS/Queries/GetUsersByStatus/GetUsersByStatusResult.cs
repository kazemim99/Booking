// 📁 Booksy.UserManagement.Application/Queries/GetUsersByStatus/GetUsersByStatusResult.cs
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Application.Queries.GetUsersByStatus
{
    public sealed class GetUsersByStatusResult
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public UserStatus Status { get; set; }
        public UserType Type { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime RegisteredAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsLocked { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
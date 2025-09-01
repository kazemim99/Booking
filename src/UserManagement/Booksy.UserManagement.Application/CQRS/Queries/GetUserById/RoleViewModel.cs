// ========================================
// Booksy.UserManagement.Application/Queries/GetUserById/GetUserByIdQuery.cs
// ========================================
// ========================================
// Booksy.UserManagement.Application/Queries/GetUserById/UserDetailsViewModel.cs
// ========================================
namespace Booksy.UserManagement.Application.CQRS.Queries.GetUserById
{
    public sealed class RoleViewModel
    {
        public string Name { get; init; } = string.Empty;
        public DateTime AssignedAt { get; init; }
        public DateTime? ExpiresAt { get; init; }
        public bool IsExpired { get; init; }
    }
}


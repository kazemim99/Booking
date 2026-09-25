// ========================================
// AsanRezerve.UserManagement.Application/Queries/GetUserById/GetUserByIdQuery.cs
// ========================================
// ========================================
// AsanRezerve.UserManagement.Application/Queries/GetUserById/UserDetailsViewModel.cs
// ========================================
namespace AsanRezerve.UserManagement.Application.CQRS.Queries.GetUserById
{
    public sealed class RoleViewModel
    {
        public string Name { get; init; } = string.Empty;
        public DateTime AssignedAt { get; init; }
        public DateTime? ExpiresAt { get; init; }
        public bool IsExpired { get; init; }
    }
}


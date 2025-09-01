// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Seeders/UserManagementDatabaseSeeder.cs
// ========================================
// ========================================
// Booksy.UserManagement.Infrastructure/ReadModels/UserReadModel.cs
// ========================================
namespace Booksy.UserManagement.Infrastructure.ReadModels
{
    /// <summary>
    /// Read model optimized for queries
    /// </summary>
    public class UserReadModel
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public DateTime RegisteredAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public string? AvatarUrl { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public int LoginCount { get; set; }
        public int ActiveSessionCount { get; set; }
    }
}

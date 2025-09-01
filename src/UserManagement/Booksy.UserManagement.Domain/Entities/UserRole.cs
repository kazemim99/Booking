// ========================================
// Booksy.UserManagement.Domain/Entities/UserRole.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.UserManagement.Domain.Entities
{
    /// <summary>
    /// Represents a role assigned to a user
    /// </summary>
    public sealed class UserRole : Entity<Guid>
    {
        public string Name { get; private set; }
        public DateTime AssignedAt { get; private set; }
        public DateTime? ExpiresAt { get; private set; }
        public string? AssignedBy { get; private set; }
        public Dictionary<string, object> Metadata { get; private set; }

        private UserRole() : base()
        {
            Metadata = new Dictionary<string, object>();
        }

        public static UserRole Create(string name, DateTime assignedAt, string? assignedBy = null, DateTime? expiresAt = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Role name cannot be empty", nameof(name));

            return new UserRole
            {
                Id = Guid.NewGuid(),
                Name = name,
                AssignedAt = assignedAt,
                AssignedBy = assignedBy,
                ExpiresAt = expiresAt,
                Metadata = new Dictionary<string, object>()
            };
        }

        public bool IsExpired()
        {
            return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
        }

        public void AddMetadata(string key, object value)
        {
            Metadata[key] = value;
        }

        public void SetExpiration(DateTime expiresAt)
        {
            if (expiresAt <= DateTime.UtcNow)
                throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));

            ExpiresAt = expiresAt;
        }
    }
}

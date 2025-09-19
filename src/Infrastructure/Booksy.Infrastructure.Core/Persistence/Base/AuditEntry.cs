// ========================================
// Booksy.Core.Application/Services/AuditEntry.cs
// ========================================
namespace Booksy.Infrastructure.Core.Persistence.Base
{
    /// <summary>
    /// Represents an audit log entry
    /// </summary>
    public sealed class AuditEntry
    {
        public Guid Id { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public DateTime Timestamp { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();

        public AuditEntry()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }

        public static AuditEntry Create(
            string entityName,
            string entityId,
            string action,
            string? userId = null,
            string? oldValues = null,
            string? newValues = null)
        {
            return new AuditEntry
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                OldValues = oldValues,
                NewValues = newValues
            };
        }

        public AuditEntry WithUser(string userId, string? email = null)
        {
            UserId = userId;
            UserEmail = email;
            return this;
        }

        public AuditEntry WithRequestInfo(string? ipAddress, string? userAgent)
        {
            IpAddress = ipAddress;
            UserAgent = userAgent;
            return this;
        }

        public AuditEntry AddMetadata(string key, object value)
        {
            Metadata[key] = value;
            return this;
        }
    }

    /// <summary>
    /// Common audit actions
    /// </summary>
    public static class AuditActions
    {
        public const string Create = "CREATE";
        public const string Update = "UPDATE";
        public const string Delete = "DELETE";
        public const string Read = "READ";
        public const string Login = "LOGIN";
        public const string Logout = "LOGOUT";
        public const string PasswordChange = "PASSWORD_CHANGE";
        public const string PasswordReset = "PASSWORD_RESET";
        public const string EmailVerification = "EMAIL_VERIFICATION";
        public const string AccessDenied = "ACCESS_DENIED";
    }
}
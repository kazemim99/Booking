// ========================================
// Booksy.UserManagement.Domain/Entities/UserRole.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Entities
{
    /// <summary>
    /// Represents a login attempt for audit and security purposes
    /// </summary>
    public sealed class LoginAttempt 
    {
        public DateTime AttemptedAt { get; private set; }
        public string? IpAddress { get; private set; }
        public string? UserAgent { get; private set; }
        public bool IsSuccessful { get; private set; }
        public string? FailureReason { get; private set; }
        public Dictionary<string, object> Metadata { get; private set; }

        private LoginAttempt() : base()
        {
            Metadata = new Dictionary<string, object>();
        }

        public static LoginAttempt Create(UserId userId, string? ipAddress = null, string? userAgent = null)
        {
            return new LoginAttempt
            {

                AttemptedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccessful = false,
                Metadata = new Dictionary<string, object>()
            };
        }

        public void MarkAsSuccessful()
        {
            IsSuccessful = true;
            FailureReason = null;
        }

        public void MarkAsFailed(string reason)
        {
            IsSuccessful = false;
            FailureReason = reason;
        }

        public void AddMetadata(string key, object value)
        {
            Metadata[key] = value;
        }

        public bool IsFromSameLocation(string? ipAddress)
        {
            return !string.IsNullOrEmpty(IpAddress) &&
                   !string.IsNullOrEmpty(ipAddress) &&
                   IpAddress.Equals(ipAddress, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsRecentAttempt(TimeSpan threshold)
        {
            return DateTime.UtcNow - AttemptedAt <= threshold;
        }
    }
}


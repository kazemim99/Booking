// ========================================
// Booksy.UserManagement.Domain/Entities/UserRole.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Entities
{
    /// <summary>
    /// Represents an active authentication session
    /// </summary>
    public sealed class AuthenticationSession 
    {
        public string SessionToken { get; private set; }
        public DateTime StartedAt { get; private set; }
        public DateTime? EndedAt { get; private set; }
        public DateTime LastActivityAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public string? IpAddress { get; private set; }
        public string? UserAgent { get; private set; }
        public string? DeviceId { get; private set; }
        public string? DeviceName { get; private set; }
        public Dictionary<string, object> Metadata { get; private set; }

        private AuthenticationSession() : base()
        {
            Metadata = new Dictionary<string, object>();
        }

        public static AuthenticationSession Create(
            string? ipAddress = null,
            string? userAgent = null,
            int sessionDurationHours = 24)
        {
            return new AuthenticationSession
            {
                SessionToken = GenerateSessionToken(),
                StartedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(sessionDurationHours),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Metadata = new Dictionary<string, object>()
            };
        }

        private static string GenerateSessionToken()
        {
            var randomBytes = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public bool IsActive => EndedAt == null && !IsExpired();

        public bool IsExpired()
        {
            return DateTime.UtcNow >= ExpiresAt;
        }

        public void UpdateActivity()
        {
            if (!IsActive)
                throw new InvalidOperationException("Cannot update activity on an inactive session");

            LastActivityAt = DateTime.UtcNow;
        }

        public void ExtendSession(int additionalHours)
        {
            if (!IsActive)
                throw new InvalidOperationException("Cannot extend an inactive session");

            ExpiresAt = ExpiresAt.AddHours(additionalHours);
            UpdateActivity();
        }

        public void End()
        {
            if (EndedAt.HasValue)
                return;

            EndedAt = DateTime.UtcNow;
        }

        public void SetDevice(string deviceId, string deviceName)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
        }

        public void AddMetadata(string key, object value)
        {
            Metadata[key] = value;
        }

        public TimeSpan GetSessionDuration()
        {
            var endTime = EndedAt ?? DateTime.UtcNow;
            return endTime - StartedAt;
        }

        public TimeSpan GetIdleTime()
        {
            return DateTime.UtcNow - LastActivityAt;
        }
    }
}
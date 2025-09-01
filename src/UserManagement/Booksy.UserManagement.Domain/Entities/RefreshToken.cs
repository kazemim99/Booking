// ========================================
// Booksy.UserManagement.Domain/Entities/UserRole.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Entities
{
    /// <summary>
    /// Represents a refresh token for JWT authentication
    /// </summary>
    public sealed class RefreshToken
    {
        public string Token { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public string? RevokedReason { get; private set; }
        public string? ReplacedByToken { get; private set; }
        public string? CreatedByIp { get; private set; }
        public string? RevokedByIp { get; private set; }

        private RefreshToken() : base() { }

        public static RefreshToken Generate(
            int expirationDays = 7,
            string? createdByIp = null)
        {
            return new RefreshToken
            {
                Token = GenerateTokenString(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
                CreatedByIp = createdByIp
            };
        }

        private static string GenerateTokenString()
        {
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public bool IsValid(string token)
        {
            return Token == token && IsActive;
        }

        public bool IsActive => RevokedAt == null && !IsExpired();

        public bool IsExpired()
        {
            return DateTime.UtcNow >= ExpiresAt;
        }

        public void Revoke(string? reason = null, string? revokedByIp = null, string? replacedByToken = null)
        {
            if (RevokedAt.HasValue)
                return;

            RevokedAt = DateTime.UtcNow;
            RevokedReason = reason;
            RevokedByIp = revokedByIp;
            ReplacedByToken = replacedByToken;
        }

        public RefreshToken Rotate(string? createdByIp = null)
        {
            var newToken = Generate( createdByIp: createdByIp);
            Revoke("Token rotation", createdByIp, newToken.Token);
            return newToken;
        }
    }
}



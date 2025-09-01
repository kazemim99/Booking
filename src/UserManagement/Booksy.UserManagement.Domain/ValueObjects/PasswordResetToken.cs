// ========================================
// Booksy.UserManagement.Domain/ValueObjects/UserId.cs
// ========================================
using Booksy.Core.Domain.Base;
using System.Security.Cryptography;

namespace Booksy.UserManagement.Domain.ValueObjects
{
    /// <summary>
    /// Represents a password reset token
    /// </summary>
    public sealed class PasswordResetToken : ValueObject
    {
        public string Token { get; }
        public DateTime CreatedAt { get; }
        public DateTime ExpiresAt { get; }

        private PasswordResetToken(string token, DateTime createdAt, DateTime expiresAt)
        {
            Token = token;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
        }

        public static PasswordResetToken Generate(int expirationHours = 2)
        {
            var token = GenerateSecureToken();
            var now = DateTime.UtcNow;
            return new PasswordResetToken(token, now, now.AddHours(expirationHours));
        }

        public static PasswordResetToken FromExisting(string token, DateTime createdAt, DateTime expiresAt)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be empty", nameof(token));

            return new PasswordResetToken(token, createdAt, expiresAt);
        }

        public bool IsValid(string token)
        {
            return Token == token && !IsExpired();
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow > ExpiresAt;
        }

        private static string GenerateSecureToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            // Use a more secure token format with timestamp
            var timestamp = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            var combined = new byte[randomBytes.Length + timestamp.Length];
            Buffer.BlockCopy(randomBytes, 0, combined, 0, randomBytes.Length);
            Buffer.BlockCopy(timestamp, 0, combined, randomBytes.Length, timestamp.Length);

            return Convert.ToBase64String(combined)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return Token;
            yield return CreatedAt;
            yield return ExpiresAt;
        }

        public override string ToString() => Token;
    }
}


// ========================================
// Booksy.UserManagement.Domain/ValueObjects/UserId.cs
// ========================================
using Booksy.Core.Domain.Base;
using System.Security.Cryptography;

namespace Booksy.UserManagement.Domain.ValueObjects
{
    /// <summary>
    /// Represents an account activation token
    /// </summary>
    public sealed class ActivationToken : ValueObject
    {
        public string Token { get; }
        public DateTime CreatedAt { get; }
        public DateTime ExpiresAt { get; }

        private ActivationToken(string token, DateTime createdAt, DateTime expiresAt)
        {
            Token = token;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
        }

        public static ActivationToken Generate(int expirationHours = 48)
        {
            var token = GenerateSecureToken();
            var now = DateTime.UtcNow;
            return new ActivationToken(token, now, now.AddHours(expirationHours));
        }

        public static ActivationToken FromExisting(string token, DateTime createdAt, DateTime expiresAt)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be empty", nameof(token));

            return new ActivationToken(token, createdAt, expiresAt);
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
            return Convert.ToBase64String(randomBytes)
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



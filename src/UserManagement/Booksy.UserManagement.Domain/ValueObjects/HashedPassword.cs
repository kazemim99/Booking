// ========================================
// Booksy.UserManagement.Domain/ValueObjects/UserId.cs
// ========================================
using BCrypt.Net;
using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.Exceptions;

namespace Booksy.UserManagement.Domain.ValueObjects
{
    /// <summary>
    /// Represents a hashed password using BCrypt
    /// </summary>
    public sealed class HashedPassword : ValueObject
    {
        private const int MinPasswordLength = 8;
        private const int MaxPasswordLength = 128;
        private const int WorkFactor = 12;

        public string Hash { get; }

        private HashedPassword(string hash)
        {
            Hash = hash;
        }

        public static HashedPassword Create(string plainPassword)
        {

            // Using BCrypt.Net-Next for password hashing
            var hash = BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);
            return new HashedPassword(hash);
        }

        public static HashedPassword FromHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Password hash cannot be empty", nameof(hash));

            return new HashedPassword(hash);
        }

        public bool Verify(string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(plainPassword))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(plainPassword, Hash);
            }
            catch
            {
                return false;
            }
        }

        

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return Hash;
        }

        public override string ToString() => "[PROTECTED]";
    }
}



// ========================================
// Security Services
// ========================================

// Booksy.UserManagement.Infrastructure/Services/Security/PasswordHasher.cs
using Booksy.UserManagement.Application.Services.Interfaces;

namespace Booksy.UserManagement.Infrastructure.Services.Security
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int WorkFactor = 12;
        private const int MinWorkFactor = 10;

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return false;
            }
        }

        public bool RequiresRehash(string hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
                return true;

            try
            {
                var hashInfo = BCrypt.Net.BCrypt.InterrogateHash(hashedPassword);
                return int.Parse(hashInfo.WorkFactor) < MinWorkFactor;
            }
            catch
            {
                return true;
            }
        }
    }
}


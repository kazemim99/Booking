// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Seeders/UserManagementDatabaseSeeder.cs
// ========================================
using Booksy.UserManagement.Domain.Services;
using System.Security.Cryptography;
using System.Text;

namespace Booksy.UserManagement.Infrastructure.Services.Domain
{
    public class PasswordPolicyService : IPasswordPolicy
    {
        private const int MinLength = 8;
        private const int MaxLength = 128;
        private readonly HashSet<string> _commonPasswords;

        public PasswordPolicyService()
        {
            _commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password", "12345678", "qwerty", "abc123", "password123",
                "admin", "letmein", "welcome", "monkey", "dragon"
            };
        }

        public IEnumerable<string> Validate(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password is required");
                return errors;
            }

            if (password.Length < MinLength)
                errors.Add($"Password must be at least {MinLength} characters long");

            if (password.Length > MaxLength)
                errors.Add($"Password cannot exceed {MaxLength} characters");

            if (!password.Any(char.IsUpper))
                errors.Add("Password must contain at least one uppercase letter");

            if (!password.Any(char.IsLower))
                errors.Add("Password must contain at least one lowercase letter");

            if (!password.Any(char.IsDigit))
                errors.Add("Password must contain at least one number");

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                errors.Add("Password must contain at least one special character");

            if (_commonPasswords.Contains(password))
                errors.Add("Password is too common");

            return errors;
        }

        public bool IsValid(string password)
        {
            return !Validate(password).Any();
        }

        public string GetPolicyDescription()
        {
            return $@"Password must:
- Be between {MinLength} and {MaxLength} characters long
- Contain at least one uppercase letter
- Contain at least one lowercase letter
- Contain at least one number
- Contain at least one special character
- Not be a common password";
        }

        public async Task<bool> IsCompromisedAsync(string password, CancellationToken cancellationToken = default)
        {
            // In production, this would check against HaveIBeenPwned API
            // For now, just check against common passwords
            await Task.Delay(10, cancellationToken); // Simulate API call
            return _commonPasswords.Contains(password.ToLower());
        }

        public string GenerateStrongPassword(int length = 16)
        {
            if (length < MinLength)
                length = MinLength;
            if (length > MaxLength)
                length = MaxLength;

            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";
            const string all = uppercase + lowercase + digits + special;

            var password = new StringBuilder();
            using var rng = RandomNumberGenerator.Create();
            var buffer = new byte[4];

            // Ensure at least one of each required character type
            password.Append(GetRandomChar(uppercase, rng, buffer));
            password.Append(GetRandomChar(lowercase, rng, buffer));
            password.Append(GetRandomChar(digits, rng, buffer));
            password.Append(GetRandomChar(special, rng, buffer));

            // Fill the rest randomly
            for (int i = 4; i < length; i++)
            {
                password.Append(GetRandomChar(all, rng, buffer));
            }

            // Shuffle the password
            return new string(password.ToString().OrderBy(_ => Guid.NewGuid()).ToArray());
        }

        private static char GetRandomChar(string chars, RandomNumberGenerator rng, byte[] buffer)
        {
            rng.GetBytes(buffer);
            var randomIndex = BitConverter.ToUInt32(buffer, 0) % chars.Length;
            return chars[(int)randomIndex];
        }
    }
}


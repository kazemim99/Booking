// ========================================
// Booksy.UserManagement.Domain/ValueObjects/OtpCode.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.UserManagement.Domain.ValueObjects
{
    /// <summary>
    /// OTP code value object with validation
    /// </summary>
    public sealed class OtpCode : ValueObject
    {
        public string Value { get; }
        public DateTime GeneratedAt { get; }
        public DateTime ExpiresAt { get; }
        public int ValidityMinutes { get; }

        private OtpCode(string value, int validityMinutes)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("OTP code cannot be empty", nameof(value));

            if (value.Length < 4 || value.Length > 8)
                throw new ArgumentException("OTP code must be between 4 and 8 digits", nameof(value));

            if (!value.All(char.IsDigit))
                throw new ArgumentException("OTP code must contain only digits", nameof(value));

            if (validityMinutes <= 0 || validityMinutes > 60)
                throw new ArgumentException("Validity must be between 1 and 60 minutes", nameof(validityMinutes));

            Value = value;
            ValidityMinutes = validityMinutes;
            GeneratedAt = DateTime.UtcNow;
            ExpiresAt = GeneratedAt.AddMinutes(validityMinutes);
        }

        public static OtpCode Create(string code, int validityMinutes = 5)
        {
            return new OtpCode(code, validityMinutes);
        }

        public static OtpCode Generate(int length = 6, int validityMinutes = 5)
        {
            if (length < 4 || length > 8)
                throw new ArgumentException("Length must be between 4 and 8 digits", nameof(length));

            var random = new Random();
            var code = string.Join("", Enumerable.Range(0, length).Select(_ => random.Next(0, 10)));

            return new OtpCode(code, validityMinutes);
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow > ExpiresAt;
        }

        public bool IsValid(string inputCode)
        {
            if (IsExpired())
                return false;

            return Value.Equals(inputCode, StringComparison.Ordinal);
        }

        public TimeSpan RemainingValidity()
        {
            if (IsExpired())
                return TimeSpan.Zero;

            return ExpiresAt - DateTime.UtcNow;
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
            yield return GeneratedAt;
            yield return ExpiresAt;
        }

        public override string ToString() => Value;

        public static implicit operator string(OtpCode otpCode) => otpCode.Value;
    }
}

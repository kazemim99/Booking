using Booksy.Core.Domain.Exceptions;

namespace Booksy.Core.Domain.Domain.Utilities
{
    /// <summary>
    /// Guard clauses for validation
    /// </summary>
    public static class Guard
    {
        public static class Against
        {
            public static string NullOrWhiteSpace(string? value, string parameterName)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new DomainValidationException($"{parameterName} cannot be null or whitespace.", parameterName);
                }

                return value;
            }

            public static T Null<T>(T? value, string parameterName) where T : class
            {
                if (value == null)
                {
                    throw new ArgumentNullException(parameterName);
                }

                return value;
            }

            public static string NullOrEmpty(string? value, string parameterName)
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new DomainValidationException($"{parameterName} cannot be null or empty.", parameterName);
                }

                return value;
            }

            public static void OutOfRange<T>(T value, T rangeFrom, T rangeTo, string parameterName)
                where T : IComparable<T>
            {
                if (value.CompareTo(rangeFrom) < 0 || value.CompareTo(rangeTo) > 0)
                {
                    throw new ArgumentOutOfRangeException(
                        parameterName,
                        $"{parameterName} must be between {rangeFrom} and {rangeTo}.");
                }
            }

            public static void NegativeOrZero(int value, string parameterName)
            {
                if (value <= 0)
                {
                    throw new DomainValidationException($"{parameterName} must be positive.", parameterName);
                }
            }

            public static void Negative(int value, string parameterName)
            {
                if (value < 0)
                {
                    throw new DomainValidationException($"{parameterName} cannot be negative.", parameterName);
                }
            }

            public static void InvalidEmail(string email, string parameterName)
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(email);
                    if (addr.Address != email)
                        throw new DomainValidationException($"{parameterName} is not a valid email address.", parameterName);
                }
                catch
                {
                    throw new DomainValidationException($"{parameterName} is not a valid email address.", parameterName);
                }
            }

            public static void InvalidLength(string value, int maxLength, string parameterName)
            {
                if (value.Length > maxLength)
                {
                    throw new DomainValidationException(
                        $"{parameterName} must not exceed {maxLength} characters.",
                        parameterName);
                }
            }

            public static void InvalidEnum<T>(T value, string parameterName) where T : Enum
            {
                if (!Enum.IsDefined(typeof(T), value))
                {
                    throw new DomainValidationException(
                        $"{parameterName} has an invalid value: {value}.",
                        parameterName);
                }
            }
        }
    }
}
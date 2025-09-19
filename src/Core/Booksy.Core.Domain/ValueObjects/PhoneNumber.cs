using System.Text.RegularExpressions;
using Booksy.Core.Domain.Base;

using Booksy.Core.Domain.Domain.Utilities;
using Booksy.Core.Domain.Exceptions;

namespace Booksy.Core.Domain.ValueObjects
{
    /// <summary>
    /// Represents a mobile phone number that starts with 09 and has exactly 11 digits
    /// </summary>
    public sealed partial class PhoneNumber : ValueObject
    {

        private PhoneNumber()
        {

        }
        private static readonly Regex PhoneRegex = PhoneNumberRegex();

        public string? Value { get; }
        public string? CountryCode { get; }
        public string? NationalNumber { get; }

        private PhoneNumber(string value, string countryCode, string nationalNumber)
        {
            Value = value;
            CountryCode = countryCode;
            NationalNumber = nationalNumber;
        }

        public static PhoneNumber From(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new DomainValidationException("PhoneNumber", "Phone number is required");

            // Remove all non-digit characters for validation
            var digitsOnly = Regex.Replace(phoneNumber, @"\D", "");

            if (digitsOnly.Length < 10 || digitsOnly.Length > 15)
                throw new DomainValidationException("PhoneNumber", "Phone number must be between 10 and 15 digits");

            // Extract country code (assuming 1-3 digits)
            var countryCode = digitsOnly.Length > 10 ? digitsOnly[..^10] : "1"; // Default to US
            var nationalNumber = digitsOnly.Length > 10 ? digitsOnly[^10..] : digitsOnly;

            // Format the number
            var formatted = FormatPhoneNumber(countryCode, nationalNumber);

            return new PhoneNumber(formatted, countryCode, nationalNumber);
        }

        private static string FormatPhoneNumber(string countryCode, string nationalNumber)
        {
            // Format as: +X (XXX) XXX-XXXX for US numbers
            if (countryCode == "1" && nationalNumber.Length == 10)
            {
                return $"+{countryCode} ({nationalNumber[..3]}) {nationalNumber.Substring(3, 3)}-{nationalNumber[6..]}";
            }

            // Generic international format
            return $"+{countryCode} {nationalNumber}";
        }

        public string GetE164Format()
        {
            return $"+{CountryCode}{NationalNumber}";
        }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return Value;
        }

        public override string? ToString() => Value;

        [GeneratedRegex(@"^\+?[1-9]\d{1,14}$")]
        private static partial Regex PhoneNumberRegex();
    }
}
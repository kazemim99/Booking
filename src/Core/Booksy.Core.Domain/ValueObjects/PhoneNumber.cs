// ========================================
// Booksy.UserManagement.Domain/ValueObjects/PhoneNumber.cs
// ========================================
using Booksy.Core.Domain.Base;
using System.Text.RegularExpressions;

namespace Booksy.Core.Domain.ValueObjects
{
    /// <summary>
    /// Phone number value object with validation
    /// </summary>
    public sealed class PhoneNumber : ValueObject
    {
        public string Value { get; }
        public string CountryCode { get; }
        public string NationalNumber { get; }


        private PhoneNumber()
        {
            Value = string.Empty;
            CountryCode = string.Empty;
            NationalNumber = string.Empty;
        }
        private PhoneNumber(string value, string countryCode, string nationalNumber)
        {
            Value = value;
            CountryCode = countryCode;
            NationalNumber = nationalNumber;
        }

        public static PhoneNumber From(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

            // Clean the phone number (remove spaces, dashes, parentheses)
            var cleaned = CleanPhoneNumber(phoneNumber);

            // Extract country code and national number
            var (countryCode, nationalNumber) = ExtractComponents(cleaned);

            // Validate
            if (!IsValid(nationalNumber))
                throw new ArgumentException($"Invalid phone number: {phoneNumber}", nameof(phoneNumber));

            return new PhoneNumber(cleaned, countryCode, nationalNumber);
        }

        public static PhoneNumber FromNational(string nationalNumber, string countryCode = "+98")
        {
            if (string.IsNullOrWhiteSpace(nationalNumber))
                throw new ArgumentException("National number cannot be empty", nameof(nationalNumber));

            var cleaned = CleanPhoneNumber(nationalNumber);

            if (!IsValid(cleaned))
                throw new ArgumentException($"Invalid national number: {nationalNumber}", nameof(nationalNumber));

            var fullNumber = countryCode + cleaned;
            return new PhoneNumber(fullNumber, countryCode, cleaned);
        }

        private static string CleanPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            // Remove all non-digit characters except '+'
            return Regex.Replace(phoneNumber, @"[^\d+]", string.Empty);
        }

        private static (string countryCode, string nationalNumber) ExtractComponents(string phoneNumber)
        {
            if (phoneNumber.StartsWith("+"))
            {
                // International format: +98xxxxxxxxxx
                if (phoneNumber.StartsWith("+98") && phoneNumber.Length >= 13)
                {
                    return ("+98", phoneNumber.Substring(3));
                }
                else if (phoneNumber.Length > 3)
                {
                    // Generic international: +XX...
                    var code = phoneNumber.Substring(0, 3);
                    var number = phoneNumber.Substring(3);
                    return (code, number);
                }
            }
            else if (phoneNumber.StartsWith("00"))
            {
                // Alternative international format: 0098xxxxxxxxxx
                if (phoneNumber.StartsWith("0098") && phoneNumber.Length >= 14)
                {
                    return ("+98", phoneNumber.Substring(4));
                }
            }
            else if (phoneNumber.StartsWith("09") && phoneNumber.Length == 11)
            {
                // Iranian national format: 09xxxxxxxxx
                return ("+98", phoneNumber.Substring(1)); // Remove leading 0
            }

            // Assume it's a national number without country code
            return ("+98", phoneNumber);
        }

        private static bool IsValid(string nationalNumber)
        {
            if (string.IsNullOrWhiteSpace(nationalNumber))
                return false;

            // Iranian mobile numbers: 9xxxxxxxxx (10 digits starting with 9)
            if (Regex.IsMatch(nationalNumber, @"^9\d{9}$"))
                return true;

            // General validation: 8-15 digits
            if (nationalNumber.Length >= 8 && nationalNumber.Length <= 15)
                return Regex.IsMatch(nationalNumber, @"^\d+$");

            return false;
        }

        public string ToInternational()
        {
            return CountryCode + NationalNumber;
        }

        public string ToNational()
        {
            // For Iranian numbers, add leading 0
            if (CountryCode == "+98" && NationalNumber.StartsWith("9"))
                return "0" + NationalNumber;

            return NationalNumber;
        }

        public string ToDisplay()
        {
            // Format for display: +98 912 345 6789
            if (CountryCode == "+98" && NationalNumber.Length == 10)
            {
                return $"{CountryCode} {NationalNumber.Substring(0, 3)} {NationalNumber.Substring(3, 3)} {NationalNumber.Substring(6)}";
            }

            return ToInternational();
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }

        public override string ToString() => ToNational();

        public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.Value;
    }
}

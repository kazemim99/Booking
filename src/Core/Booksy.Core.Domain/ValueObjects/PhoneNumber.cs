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
            if (!IsValid(countryCode, nationalNumber))
                throw new ArgumentException($"Invalid phone number: {phoneNumber}", nameof(phoneNumber));

            // Value is always the canonical E.164 form. Persisting the raw input
            // instead would let one real number have several stored representations
            // (local "09…" vs "+98…"), which silently breaks every lookup and
            // uniqueness check that compares on Value.
            return new PhoneNumber(countryCode + nationalNumber, countryCode, nationalNumber);
        }

        public static PhoneNumber FromNational(string nationalNumber, string countryCode = "+98")
        {
            if (string.IsNullOrWhiteSpace(nationalNumber))
                throw new ArgumentException("National number cannot be empty", nameof(nationalNumber));

            var cleaned = StripTrunkPrefix(CleanPhoneNumber(nationalNumber), countryCode);

            if (!IsValid(countryCode, cleaned))
                throw new ArgumentException($"Invalid national number: {nationalNumber}", nameof(nationalNumber));

            return new PhoneNumber(countryCode + cleaned, countryCode, cleaned);
        }

        private static string CleanPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            // Remove all non-digit characters except '+'
            return Regex.Replace(phoneNumber, @"[^\d+]", string.Empty);
        }

        private const string IranCountryCode = "+98";

        /// <summary>
        /// Iranian national numbers are 10 digits (9xxxxxxxxx); prefixed with the
        /// country code that is 12. Used to tell a bare "98…" country-code prefix
        /// apart from a national number that merely happens to start with 98.
        /// </summary>
        private const int IranNationalLength = 10;

        private static (string countryCode, string nationalNumber) ExtractComponents(string phoneNumber)
        {
            // Normalize the international access code (00XX) to the plus form.
            if (phoneNumber.StartsWith("00") && phoneNumber.Length > 4)
                phoneNumber = "+" + phoneNumber.Substring(2);

            if (phoneNumber.StartsWith("+"))
            {
                // Iranian: +98 followed by the national number, which callers
                // sometimes pass still carrying its trunk zero (+980912…).
                if (phoneNumber.StartsWith(IranCountryCode) && phoneNumber.Length > 3)
                {
                    return (IranCountryCode,
                        StripTrunkPrefix(phoneNumber.Substring(3), IranCountryCode));
                }

                if (phoneNumber.Length > 3)
                {
                    // Generic international: +XX...
                    var code = phoneNumber.Substring(0, 3);
                    var number = phoneNumber.Substring(3);
                    return (code, number);
                }
            }
            else if (phoneNumber.StartsWith("98")
                     && phoneNumber.Length == IranNationalLength + 2)
            {
                // Country code without the plus: 989xxxxxxxxx
                return (IranCountryCode, phoneNumber.Substring(2));
            }

            // Bare national number, with or without the trunk zero.
            return (IranCountryCode, StripTrunkPrefix(phoneNumber, IranCountryCode));
        }

        /// <summary>
        /// Removes the national trunk prefix ("0") that precedes the subscriber
        /// number in local dialling formats — Iranian numbers are written
        /// 09121234567 locally but 9121234567 in E.164.
        /// </summary>
        private static string StripTrunkPrefix(string nationalNumber, string countryCode)
        {
            if (countryCode != IranCountryCode)
                return nationalNumber;

            return nationalNumber.Length == IranNationalLength + 1
                   && nationalNumber.StartsWith("0")
                ? nationalNumber.Substring(1)
                : nationalNumber;
        }

        private static bool IsValid(string countryCode, string nationalNumber)
        {
            if (string.IsNullOrWhiteSpace(nationalNumber))
                return false;

            // Iranian mobile numbers: 9xxxxxxxxx (10 digits starting with 9)
            if (countryCode == IranCountryCode)
                return Regex.IsMatch(nationalNumber, @"^9\d{9}$");

            // General validation: 8-15 digits
            if (nationalNumber.Length >= 8 && nationalNumber.Length <= 15)
                return Regex.IsMatch(nationalNumber, @"^\d+$");

            return false;
        }

        public string ToInternational()
        {
            return CountryCode + NationalNumber;
        }

        /// <summary>
        /// Every string form this number may already be stored as. Rows written
        /// before <see cref="Value"/> was canonicalized hold the raw input the
        /// caller happened to send (local "09…" from the mobile apps, "+98…"
        /// from the E2E scripts), so persistence lookups must match against all
        /// of these rather than the canonical value alone.
        /// </summary>
        public IReadOnlyCollection<string> EquivalentForms()
        {
            var forms = new List<string>
            {
                Value,                              // canonical: +989121234567
                CountryCode + NationalNumber,       // explicit international
                NationalNumber,                     // bare national: 9121234567
            };

            if (CountryCode.StartsWith("+"))
                forms.Add(CountryCode.Substring(1) + NationalNumber); // 989121234567

            if (CountryCode == IranCountryCode)
                forms.Add("0" + NationalNumber);    // legacy local: 09121234567

            return forms.Distinct().ToList();
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

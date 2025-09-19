// ========================================
// Booksy.UserManagement.Infrastructure/Testing/Builders/UserBuilder.cs
// ========================================

using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Entities;

namespace Booksy.UserManagement.Infrastructure.Testing.Builders
{
    /// <summary>
    /// Builder for creating UserProfile instances for testing
    /// </summary>
    public class UserProfileBuilder
    {
        private string _firstName = "John";
        private string _lastName = "Doe";
        private string? _middleName;
        private DateTime? _dateOfBirth;
        private string? _gender;
        private PhoneNumber? _phoneNumber;
        private PhoneNumber? _alternatePhoneNumber;
        private Address? _address;
        private string? _avatarUrl;
        private string? _bio;
        private string? _preferredLanguage;
        private string? _timeZone;
        private readonly Dictionary<string, string> _preferences = new();

        private UserProfileBuilder() { }

        public static UserProfileBuilder Create()
        {
            return new UserProfileBuilder();
        }

        public UserProfileBuilder WithName(string firstName, string lastName, string? middleName = null)
        {
            _firstName = firstName;
            _lastName = lastName;
            _middleName = middleName;
            return this;
        }

        public UserProfileBuilder WithPersonalInfo(DateTime? dateOfBirth, string? gender)
        {
            _dateOfBirth = dateOfBirth;
            _gender = gender;
            return this;
        }

        public UserProfileBuilder WithPhoneNumber(string phoneNumber)
        {
            _phoneNumber = PhoneNumber.From(phoneNumber);
            return this;
        }

        public UserProfileBuilder WithAlternatePhoneNumber(string phoneNumber)
        {
            _alternatePhoneNumber = PhoneNumber.From(phoneNumber);
            return this;
        }

        public UserProfileBuilder WithAddress(
            string street,
            string city,
            string state,
            string postalCode,
            string country,
            string? unit = null)
        {
            _address = Address.Create(street, city, state, postalCode, country, unit);
            return this;
        }

        public UserProfileBuilder WithAvatar(string avatarUrl)
        {
            _avatarUrl = avatarUrl;
            return this;
        }

        public UserProfileBuilder WithBio(string bio)
        {
            _bio = bio;
            return this;
        }

        public UserProfileBuilder WithLocalization(string? language, string? timeZone)
        {
            _preferredLanguage = language;
            _timeZone = timeZone;
            return this;
        }

        public UserProfileBuilder WithPreference(string key, string value)
        {
            _preferences[key] = value;
            return this;
        }

        public UserProfile Build()
        {
            var profile = UserProfile.Create(
                _firstName,
                _lastName,
                _middleName,
                _dateOfBirth,
                _gender);

            if (_phoneNumber != null || _alternatePhoneNumber != null || _address != null)
            {
                profile.UpdateContactInfo(_phoneNumber, _alternatePhoneNumber, _address);
            }

            if (!string.IsNullOrEmpty(_avatarUrl))
            {
                profile.UpdateAvatar(_avatarUrl);
            }

            if (!string.IsNullOrEmpty(_bio))
            {
                profile.UpdateBio(_bio);
            }

            if (!string.IsNullOrEmpty(_preferredLanguage) || !string.IsNullOrEmpty(_timeZone))
            {
                profile.UpdateLocalizationSettings(_preferredLanguage, _timeZone);
            }

            foreach (var preference in _preferences)
            {
                profile.SetPreference(preference.Key, preference.Value);
            }

            return profile;
        }

        /// <summary>
        /// Creates a complete profile with all fields populated
        /// </summary>
        public static UserProfile BuildComplete()
        {
            return Create()
                .WithName("John", "Doe", "Michael")
                .WithPersonalInfo(new DateTime(1990, 1, 1), "Male")
                .WithPhoneNumber("+15551234567")
                .WithAlternatePhoneNumber("+15559876543")
                .WithAddress("123 Main St", "New York", "NY", "10001", "USA", "Apt 4B")
                .WithAvatar("https://example.com/avatar.jpg")
                .WithBio("Software developer with 10 years of experience")
                .WithLocalization("en-US", "America/New_York")
                .WithPreference("theme", "dark")
                .WithPreference("notifications", "enabled")
                .Build();
        }
    }
}
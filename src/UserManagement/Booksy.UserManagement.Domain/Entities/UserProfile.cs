// ========================================
// Booksy.UserManagement.Domain/Entities/UserRole.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Entities
{
    /// <summary>
    /// Represents a user's profile information
    /// </summary>
    public sealed class UserProfile : Entity<Guid>
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string? MiddleName { get; private set; }
        public DateTime? DateOfBirth { get; private set; }
        public string? Gender { get; private set; }
        public PhoneNumber? PhoneNumber { get; private set; }
        public PhoneNumber? AlternatePhoneNumber { get; private set; }
        public Address? Address { get; private set; }
        public string? AvatarUrl { get; private set; }
        public string? Bio { get; private set; }
        public string? PreferredLanguage { get; private set; }
        public string? TimeZone { get; private set; }
        public Dictionary<string, string> Preferences { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public UserId UserId { get; set; }

        private UserProfile() : base()
        {
            Preferences = new Dictionary<string, string>();
        }

        public static UserProfile Create(
            string firstName,
            string lastName,
            string? middleName = null,
            DateTime? dateOfBirth = null,
            string? gender = null)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First name is required", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name is required", nameof(lastName));

            return new UserProfile
            {
                Id = Guid.NewGuid(),
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                MiddleName = middleName?.Trim(),
                DateOfBirth = dateOfBirth,
                Gender = gender?.Trim(),
                CreatedAt = DateTime.UtcNow,
                Preferences = new Dictionary<string, string>()
            };
        }

        public string GetFullName()
        {
            var parts = new List<string> { FirstName };

            if (!string.IsNullOrWhiteSpace(MiddleName))
                parts.Add(MiddleName);

            parts.Add(LastName);

            return string.Join(" ", parts);
        }

        public string GetDisplayName()
        {
            return $"{FirstName} {LastName}";
        }

        public int? GetAge()
        {
            if (!DateOfBirth.HasValue)
                return null;

            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Value.Year;

            if (DateOfBirth.Value.Date > today.AddYears(-age))
                age--;

            return age;
        }

        public void UpdatePersonalInfo(
            string firstName,
            string lastName,
            string? middleName = null,
            DateTime? dateOfBirth = null,
            string? gender = null)
        {
            FirstName = firstName?.Trim() ?? FirstName;
            LastName = lastName?.Trim() ?? LastName;
            MiddleName = middleName?.Trim();
            DateOfBirth = dateOfBirth;
            Gender = gender?.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateContactInfo(PhoneNumber? phoneNumber, PhoneNumber? alternatePhoneNumber, Address? address)
        {
            PhoneNumber = phoneNumber;
            AlternatePhoneNumber = alternatePhoneNumber;
            Address = address;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateAvatar(string? avatarUrl)
        {
            AvatarUrl = avatarUrl;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateBio(string? bio)
        {
            Bio = bio?.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateLocalizationSettings(string? preferredLanguage, string? timeZone)
        {
            PreferredLanguage = preferredLanguage;
            TimeZone = timeZone;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetPreference(string key, string value)
        {
            Preferences[key] = value;
            UpdatedAt = DateTime.UtcNow;
        }

        public string? GetPreference(string key)
        {
            return Preferences.TryGetValue(key, out var value) ? value : null;
        }

        public void UpdateName(string firstName, string lastName, string? middleName)
        {
            this.FirstName = firstName;
            this.LastName = lastName;
            this.MiddleName = middleName;
        }

        public void UpdateDateOfBirth(DateTime value)
        {
            this.DateOfBirth = value;
        }

        public void UpdateGender(string gender)
        {
            this.Gender = gender;
        }
    }
}


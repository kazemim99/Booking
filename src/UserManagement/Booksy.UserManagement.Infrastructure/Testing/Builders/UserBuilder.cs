// ========================================
// Booksy.UserManagement.Infrastructure/Testing/Builders/UserBuilder.cs
// ========================================

using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Infrastructure.Testing.Builders
{
    /// <summary>
    /// Builder for creating User instances for testing and seeding
    /// This builder should ONLY be used for testing and database seeding, never in production code
    /// </summary>
    public class UserBuilder
    {
        private Email _email = Email.From("test@example.com");
        private HashedPassword _password = HashedPassword.Create("Test@123!");
        private UserProfile? _profile;
        private UserType _type = UserType.Customer;
        private UserStatus _status = UserStatus.Pending;
        private DateTime? _registeredAt;
        private DateTime? _activatedAt;
        private DateTime? _lastLoginAt;
        private DateTime? _lastPasswordChangeAt;
        private bool _twoFactorEnabled = false;
        private readonly List<string> _roles = new();
        private readonly Dictionary<string, string> _preferences = new();
        private int _loginCount = 0;
        private bool _clearEvents = true;

        // Private constructor to enforce factory method usage
        private UserBuilder() { }

        /// <summary>
        /// Creates a new UserBuilder instance
        /// </summary>
        public static UserBuilder Create()
        {
            return new UserBuilder();
        }

        /// <summary>
        /// Sets the user's email
        /// </summary>
        public UserBuilder WithEmail(string email)
        {
            _email = Email.From(email);
            return this;
        }

        /// <summary>
        /// Sets the user's password
        /// </summary>
        public UserBuilder WithPassword(string password)
        {
            _password = HashedPassword.Create(password);
            return this;
        }

        /// <summary>
        /// Sets the user's profile
        /// </summary>
        public UserBuilder WithProfile(string firstName, string lastName, DateTime? dateOfBirth = null, string? gender = null)
        {
            _profile = UserProfile.Create(firstName, lastName, null, dateOfBirth, gender);
            return this;
        }

        /// <summary>
        /// Sets the user's profile with full details
        /// </summary>
        public UserBuilder WithFullProfile(UserProfile profile)
        {
            _profile = profile;
            return this;
        }

        /// <summary>
        /// Sets the user's type
        /// </summary>
        public UserBuilder WithType(UserType type)
        {
            _type = type;
            return this;
        }

        /// <summary>
        /// Makes the user active (pre-activated)
        /// </summary>
        public UserBuilder AsActive(DateTime? activatedAt = null)
        {
            _status = UserStatus.Active;
            _activatedAt = activatedAt ?? DateTime.UtcNow;
            return this;
        }

        /// <summary>
        /// Makes the user pending (default)
        /// </summary>
        public UserBuilder AsPending()
        {
            _status = UserStatus.Pending;
            _activatedAt = null;
            return this;
        }

        /// <summary>
        /// Sets the registration date
        /// </summary>
        public UserBuilder RegisteredAt(DateTime registeredAt)
        {
            _registeredAt = registeredAt;
            return this;
        }

        /// <summary>
        /// Sets the last login date and optionally login history
        /// </summary>
        public UserBuilder WithLoginHistory(DateTime? lastLoginAt = null, int loginCount = 0)
        {
            _lastLoginAt = lastLoginAt;
            _loginCount = loginCount;
            return this;
        }

        /// <summary>
        /// Adds a role to the user
        /// </summary>
        public UserBuilder WithRole(string roleName)
        {
            _roles.Add(roleName);
            return this;
        }

        /// <summary>
        /// Adds multiple roles to the user
        /// </summary>
        public UserBuilder WithRoles(params string[] roleNames)
        {
            _roles.AddRange(roleNames);
            return this;
        }

        /// <summary>
        /// Sets a user preference
        /// </summary>
        public UserBuilder WithPreference(string key, string value)
        {
            _preferences[key] = value;
            return this;
        }

        /// <summary>
        /// Enables two-factor authentication
        /// </summary>
        public UserBuilder WithTwoFactorEnabled()
        {
            _twoFactorEnabled = true;
            return this;
        }

        /// <summary>
        /// Sets whether to clear domain events after building (default: true for test data)
        /// </summary>
        public UserBuilder KeepDomainEvents(bool keep = true)
        {
            _clearEvents = !keep;
            return this;
        }

        /// <summary>
        /// Builds the User instance
        /// </summary>
        public User Build()
        {
            // Ensure we have a profile
            if (_profile == null)
            {
                _profile = UserProfile.Create("Test", "User");
            }

            // Apply preferences to profile
            foreach (var preference in _preferences)
            {
                _profile.SetPreference(preference.Key, preference.Value);
            }

            User user;

            // Create user based on status
            if (_status == UserStatus.Active)
            {
                user = User.CreateActivated(
                    _email,
                    _password,
                    _profile,
                    _type,
                    _activatedAt);

                // Set registration date if specified
                if (_registeredAt.HasValue)
                {
                    // Use reflection for test data only - never do this in production
                    typeof(User).GetProperty(nameof(User.RegisteredAt))!
                        .GetSetMethod(true)!
                        .Invoke(user, new object[] { _registeredAt.Value });
                }

                // Simulate login history if specified
                if (_lastLoginAt.HasValue || _loginCount > 0)
                {
                    user.SimulateLoginHistory(_lastLoginAt, _loginCount);
                }
            }
            else
            {
                user = User.Register(
                    _email,
                    _password,
                    _profile,
                    _type);

                // Set registration date if specified
                if (_registeredAt.HasValue)
                {
                    typeof(User).GetProperty(nameof(User.RegisteredAt))!
                        .GetSetMethod(true)!
                        .Invoke(user, new object[] { _registeredAt.Value });
                }
            }

            // Add roles
            foreach (var role in _roles)
            {
                user.AddRole(role);
            }

            // Enable two-factor if specified
            if (_twoFactorEnabled)
            {
                user.EnableTwoFactor();
            }

            // Clear domain events for test data
            if (_clearEvents)
            {
                user.ClearDomainEvents();
            }

            return user;
        }

        /// <summary>
        /// Builds and returns the user with common admin setup
        /// </summary>
        public static User BuildAdmin(string email = "admin@booksy.com")
        {
            return Create()
                .WithEmail(email)
                .WithPassword("Admin@123!")
                .WithProfile("System", "Administrator")
                .WithType(UserType.Admin)
                .AsActive(DateTime.UtcNow.AddDays(-365))
                .RegisteredAt(DateTime.UtcNow.AddDays(-400))
                .WithRoles("Administrator", "Support")
                .WithLoginHistory(DateTime.UtcNow.AddMinutes(-30), 100)
                .Build();
        }

        /// <summary>
        /// Builds and returns a customer user
        /// </summary>
        public static User BuildCustomer(
            string email,
            string firstName,
            string lastName,
            bool isActive = true)
        {
            var builder = Create()
                .WithEmail(email)
                .WithPassword($"{firstName}@123")
                .WithProfile(firstName, lastName)
                .WithType(UserType.Customer)
                .WithRole("Customer")
                .WithPreference("notifications", "enabled")
                .WithPreference("language", "en");

            if (isActive)
            {
                builder.AsActive(DateTime.UtcNow.AddDays(-30))
                    .WithLoginHistory(DateTime.UtcNow.AddHours(-1), 5);
            }
            else
            {
                builder.AsPending();
            }

            return builder.Build();
        }

        /// <summary>
        /// Builds and returns a provider user
        /// </summary>
        public static User BuildProvider(
            string email,
            string businessName,
            Address? address = null)
        {
            var profile = UserProfile.Create(businessName, "Provider");

            if (address != null)
            {
                profile.UpdateContactInfo(
                    PhoneNumber.From("+15551234567"),
                    null,
                    address);
            }

            profile.UpdateBio($"Professional service provider: {businessName}");

            return Create()
                .WithEmail(email)
                .WithPassword($"{businessName}@123")
                .WithFullProfile(profile)
                .WithType(UserType.Provider)
                .AsActive(DateTime.UtcNow.AddDays(-60))
                .WithRoles("Provider", "Premium")
                .WithLoginHistory(DateTime.UtcNow.AddDays(-1), 20)
                .Build();
        }
    }
}


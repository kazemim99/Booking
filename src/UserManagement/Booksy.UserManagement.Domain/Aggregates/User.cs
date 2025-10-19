// ========================================
// Booksy.UserManagement.Domain/Aggregates/UserAggregate/User.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Events;
using Booksy.UserManagement.Domain.Exceptions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Booksy.UserManagement.Domain.Aggregates
{
    /// <summary>
    /// User aggregate root - manages user identity, authentication, and profile
    /// </summary>
    public sealed class User : AggregateRoot<UserId>, IAuditableEntity
    {
        private readonly List<UserRole> _roles = new();
        private readonly List<LoginAttempt> _recentLoginAttempts = new();
        private readonly List<RefreshToken> _refreshTokens = new();
        private readonly List<AuthenticationSession> _activeSessions = new();

        // Core Identity
        public Email Email { get; private set; }
        public HashedPassword Password { get; private set; }
        public UserProfile Profile { get; private set; }

        // Phone Verification
        public PhoneNumber? PhoneNumber { get; private set; }
        public bool PhoneNumberVerified { get; private set; }
        public DateTime? PhoneVerifiedAt { get; private set; }

        // Status & Type
        public UserStatus Status { get; private set; }
        public UserType Type { get; private set; }

        // Timestamps
        public DateTime RegisteredAt { get; private set; }
        public DateTime? ActivatedAt { get; private set; }
        public DateTime? LastLoginAt { get; private set; }
        public DateTime? LastPasswordChangeAt { get; private set; }
        public DateTime? DeactivatedAt { get; private set; }

        // Security Tokens
        public ActivationToken? ActivationToken { get; private set; }
        public PasswordResetToken? PasswordResetToken { get; private set; }

        // Security Settings
        public bool TwoFactorEnabled { get; private set; }
        public int FailedLoginAttempts { get; private set; }
        public DateTime? LockedUntil { get; private set; }

        // Collections
        public IReadOnlyList<UserRole> Roles => _roles.AsReadOnly();
        public IReadOnlyList<LoginAttempt> RecentLoginAttempts => _recentLoginAttempts.AsReadOnly();
        public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
        public IReadOnlyList<AuthenticationSession> ActiveSessions => _activeSessions.AsReadOnly();

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }

        // Private constructor for EF Core
        private User() : base() { }


        public static User Register(Email email,
            HashedPassword password,
            UserType type = UserType.Customer)
        {
            var user = new User
            {
                Id = UserId.CreateNew(),
                Email = email,
                Password = password,
                Type = type,
                Status = UserStatus.Pending,
                RegisteredAt = DateTime.UtcNow,
                FailedLoginAttempts = 0,
                TwoFactorEnabled = false,
                ActivationToken = ActivationToken.Generate()
            };

            return user;
        }
        // Factory method for creating new users
        public static User Register(
            Email email,
            HashedPassword password,
            UserProfile profile,
            UserType type = UserType.Customer)
        {
            var user = new User
            {
                Id = UserId.CreateNew(),
                Email = email,
                Password = password,
                Profile = profile,
                Type = type,
                Status = UserStatus.Pending,
                RegisteredAt = DateTime.UtcNow,
                FailedLoginAttempts = 0,
                TwoFactorEnabled = false,
                ActivationToken = ActivationToken.Generate()
            };

            // Add default role based on user type
            user._roles.Add(UserRole.Create(
                type == UserType.Customer ? "Customer" : "Provider",
                DateTime.UtcNow));

            user.RaiseDomainEvent(new UserRegisteredEvent(
                user.Id,
                user.Email,
                user.Profile.FirstName,
                user.Profile.LastName,
                user.Type,
                user.RegisteredAt));

            return user;
        }

        // Factory method for creating pre-activated users (for testing/seeding only)
        public static User CreateActivated(
            Email email,
            HashedPassword password,
            UserProfile profile,
            UserType type = UserType.Customer,
            DateTime? activatedAt = null)
        {
            var user = new User
            {
                Id = UserId.CreateNew(),
                Email = email,
                Password = password,
                Profile = profile,
                Type = type,
                Status = UserStatus.Active,
                RegisteredAt = activatedAt?.AddDays(-1) ?? DateTime.UtcNow.AddDays(-1),
                ActivatedAt = activatedAt ?? DateTime.UtcNow,
                FailedLoginAttempts = 0,
                TwoFactorEnabled = false,
                ActivationToken = null // No activation token for pre-activated users
            };

            // Add default role based on user type
            user._roles.Add(UserRole.Create(
                type == UserType.Customer ? "Customer" : "Provider",
                user.RegisteredAt));

            // Don't raise domain events for seeded data
            user.ClearDomainEvents();

            return user;
        }

        // Activation
        public void Activate(string token)
        {
            EnsureValidState(
                () => Status == UserStatus.Pending,
                "Activate",
                Status.ToString());

            if (ActivationToken == null || !ActivationToken.IsValid(token))
                throw new InvalidUserProfileException("Invalid or expired activation token");

            Status = UserStatus.Active;
            ActivatedAt = DateTime.UtcNow;
            ActivationToken = null;

            RaiseDomainEvent(new UserActivatedEvent(Id, Email, ActivatedAt.Value));
        }

        // Authentication
        public AuthenticationResult Authenticate(string password, string? ipAddress = null, string? userAgent = null)
        {

            if (Status != UserStatus.Active)
                throw new InvalidCredentialsException("User account is not active");

            if (IsLocked())
                throw new InvalidCredentialsException($"Account is locked until {LockedUntil:yyyy-MM-dd HH:mm:ss} UTC");

            var loginAttempt = LoginAttempt.Create(Id,ipAddress, userAgent);
            _recentLoginAttempts.Add(loginAttempt);

            // Keep only last 10 login attempts
            if (_recentLoginAttempts.Count > 10)
            {
                _recentLoginAttempts.RemoveAt(0);
            }

            if (!Password.Verify(password))
            {
                FailedLoginAttempts++;
                loginAttempt.MarkAsFailed("Invalid password");

                // Lock account after 5 failed attempts
                if (FailedLoginAttempts >= 5)
                {
                    LockAccount(TimeSpan.FromMinutes(30));
                }

                throw new InvalidCredentialsException("Invalid email or password");
            }

            // Successful authentication
            FailedLoginAttempts = 0;
            LastLoginAt = DateTime.UtcNow;
            loginAttempt.MarkAsSuccessful();

            var refreshToken = RefreshToken.Generate();
            _refreshTokens.Add(refreshToken);

            var session = AuthenticationSession.Create(ipAddress, userAgent);
            _activeSessions.Add(session);

            RaiseDomainEvent(new UserAuthenticatedEvent(
                Id,
                Email,
                LastLoginAt.Value,
                ipAddress,
                userAgent));



            return AuthenticationResult.Success(
                Id,
                Email,
                Profile.GetDisplayName(),
                Roles.Select(r => r.Name).ToList(),
                "");
        }

        // Password Management
        public void ChangePassword(string currentPassword, string newPassword)
        {
            EnsureValidState(
                () => Status == UserStatus.Active,
                "ChangePassword",
                Status.ToString());

            if (!Password.Verify(currentPassword))
                throw new InvalidCredentialsException("Current password is incorrect");

            var newHashedPassword = HashedPassword.Create(newPassword);
            Password = newHashedPassword;
            LastPasswordChangeAt = DateTime.UtcNow;

            // Invalidate all refresh tokens and sessions for security
            _refreshTokens.Clear();
            _activeSessions.Clear();

            RaiseDomainEvent(new PasswordChangedEvent(Id, LastPasswordChangeAt.Value));
        }

        public void RequestPasswordReset()
        {
            EnsureValidState(
                () => Status == UserStatus.Active,
                "RequestPasswordReset",
                Status.ToString());

            PasswordResetToken = PasswordResetToken.Generate();

            RaiseDomainEvent(new PasswordResetRequestedEvent(
                Id,
                Email,
                PasswordResetToken.Token,
                PasswordResetToken.ExpiresAt));
        }

        public void ResetPassword(string token, string newPassword)
        {
            if (PasswordResetToken == null || !PasswordResetToken.IsValid(token))
                throw new InvalidUserProfileException("Invalid or expired password reset token");

            Password = HashedPassword.Create(newPassword);
            LastPasswordChangeAt = DateTime.UtcNow;
            PasswordResetToken = null;

            // Invalidate all sessions for security
            _refreshTokens.Clear();
            _activeSessions.Clear();

            RaiseDomainEvent(new PasswordChangedEvent(Id, LastPasswordChangeAt.Value));
        }

        // Two-Factor Authentication
        public void EnableTwoFactor()
        {
            if (TwoFactorEnabled)
                return;

            TwoFactorEnabled = true;
            RaiseDomainEvent(new TwoFactorEnabledEvent(Id, DateTime.UtcNow));
        }

        public void DisableTwoFactor()
        {
            if (!TwoFactorEnabled)
                return;

            TwoFactorEnabled = false;
            RaiseDomainEvent(new TwoFactorDisabledEvent(Id, DateTime.UtcNow));
        }

        // Account Management
        public void Deactivate(string reason)
        {
            EnsureValidState(
                () => Status == UserStatus.Active,
                "Deactivate",
                Status.ToString());

            Status = UserStatus.Inactive;
            DeactivatedAt = DateTime.UtcNow;

            // Clear all sessions and tokens
            _refreshTokens.Clear();
            _activeSessions.Clear();

            RaiseDomainEvent(new UserDeactivatedEvent(Id, reason, DeactivatedAt.Value));
        }

        public void Reactivate()
        {
            EnsureValidState(
                () => Status == UserStatus.Inactive,
                "Reactivate",
                Status.ToString());

            Status = UserStatus.Active;
            DeactivatedAt = null;

            RaiseDomainEvent(new UserReactivatedEvent(Id, DateTime.UtcNow));
        }

        public void Suspend(string reason, DateTime until)
        {
            EnsureValidState(
                () => Status == UserStatus.Active,
                "Suspend",
                Status.ToString());

            Status = UserStatus.Suspended;
            LockedUntil = until;

            RaiseDomainEvent(new UserDeactivatedEvent(Id, reason, DateTime.UtcNow));
        }

        // Method for simulating login history (for seeding/testing only)
        public void SimulateLoginHistory(DateTime? lastLoginAt = null, int loginCount = 0)
        {
            if (Status != UserStatus.Active)
                throw new InvalidOperationException("Can only simulate login history for active users");

            LastLoginAt = lastLoginAt ?? DateTime.UtcNow.AddHours(-1);

            // Add some login attempts for history
            for (int i = 0; i < Math.Min(loginCount, 5); i++)
            {
                var attempt = LoginAttempt.Create(Id,"127.0.0.1", "Test User Agent");
                attempt.MarkAsSuccessful();
                _recentLoginAttempts.Add(attempt);
            }

            // Don't raise events for simulated data
            ClearDomainEvents();
        }

        // Profile Management
        public void UpdateProfile(UserProfile newProfile)
        {
            Profile = newProfile ?? throw new ArgumentNullException(nameof(newProfile));
        }

        // Role Management
        public void AddRole(string roleName)
        {
            if (_roles.Any(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                return;

            _roles.Add(UserRole.Create(roleName, DateTime.UtcNow));
        }

        public void RemoveRole(string roleName)
        {
            _roles.RemoveAll(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasRole(string roleName)
        {
            return _roles.Any(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }

        // Token Management
        public RefreshToken? GetValidRefreshToken(string token)
        {
            return _refreshTokens.FirstOrDefault(rt => rt.IsValid(token));
        }

        public void RevokeRefreshToken(string token)
        {
            var refreshToken = _refreshTokens.FirstOrDefault(rt => rt.Token == token);
            refreshToken?.Revoke();
        }

        public void AddRefreshToken(RefreshToken refreshToken)
        {
            _refreshTokens.Add(refreshToken);
        }
        public void RevokeAllRefreshTokens()
        {
            foreach (var token in _refreshTokens)
            {
                token.Revoke();
            }
        }


        public void EndAllSessions()
        {
            foreach (var session in _activeSessions)
            {
                session.End();
            }
        }

        // Account Locking
        private void LockAccount(TimeSpan duration)
        {
            LockedUntil = DateTime.UtcNow.Add(duration);
        }

        public bool IsLocked()
        {
            return LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
        }

        public void UnlockAccount()
        {
            LockedUntil = null;
            FailedLoginAttempts = 0;
        }

        // Clean up expired tokens and sessions
        public void CleanupExpiredData()
        {
            _refreshTokens.RemoveAll(rt => rt.IsExpired());
            _activeSessions.RemoveAll(s => !s.IsActive);
        }

        public void UpdateRoles(IReadOnlyList<UserRole> roles)
        {
            throw new NotImplementedException();
        }

        public void UpdateRefreshTokens(IReadOnlyList<RefreshToken> refreshTokens)
        {
            throw new NotImplementedException();
        }

        // ========================================
        // Phone-Based Authentication Methods
        // ========================================

        /// <summary>
        /// Creates a new user from phone verification (passwordless registration)
        /// </summary>
        public static User CreateFromPhoneVerification(
            UserId id,
            PhoneNumber phoneNumber,
            UserType userType,
            string firstName,
            string lastName)
        {
            var user = new User
            {
                Id = id,
                
                Email = Email.Create($"{phoneNumber.Value.Replace("+0 ", "a")}@temp.booksy.com"), // Temporary email
                Password = HashedPassword.Create(Guid.NewGuid().ToString()), // Random password (not used)
                Profile = UserProfile.Create(firstName, lastName, phoneNumber.Value),
                PhoneNumber = phoneNumber,
                PhoneNumberVerified = true,
                PhoneVerifiedAt = DateTime.UtcNow,
                Type = userType,
                Status = UserStatus.Draft, // Phone-verified users are automatically active
                RegisteredAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                FailedLoginAttempts = 0,
                TwoFactorEnabled = false,
                ActivationToken = null // No email activation needed

            };

            // Don't raise domain events yet (optional: can add PhoneVerifiedRegistrationEvent)
            return user;
        }

        /// <summary>
        /// Marks phone number as verified
        /// </summary>
        public void VerifyPhoneNumber()
        {
            if (PhoneNumberVerified)
                return;

            PhoneNumberVerified = true;
            PhoneVerifiedAt = DateTime.UtcNow;

            // If user was pending, activate them
            if (Status == UserStatus.Pending)
            {
                Status = UserStatus.Active;
                ActivatedAt = DateTime.UtcNow;
            }

            RaiseDomainEvent(new UserActivatedEvent(Id, Email, DateTime.UtcNow));
        }

        /// <summary>
        /// Sets phone number for existing user
        /// </summary>
        public void SetPhoneNumber(PhoneNumber phoneNumber)
        {
            PhoneNumber = phoneNumber;
            PhoneNumberVerified = false;
            PhoneVerifiedAt = null;
        }

        public void SetStatus(UserStatus pending)
        {
            this.Status = pending;
        }
    }
}
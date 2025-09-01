
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.ValueObjects
{
    /// <summary>
    /// Represents the result of an authentication attempt
    /// </summary>
    public sealed class AuthenticationResult : ValueObject
    {
        public bool IsSuccess { get; }
        public UserId? UserId { get; }
        public Email? Email { get; }
        public string? DisplayName { get; }
        public IReadOnlyList<string> Roles { get; }
        public string? AccessToken { get; }
        public string? RefreshToken { get; }
        public string? FailureReason { get; }
        public Dictionary<string, object> Claims { get; }

        private AuthenticationResult(
            bool isSuccess,
            UserId? userId = null,
            Email? email = null,
            string? displayName = null,
            IEnumerable<string>? roles = null,
            string? accessToken = null,
            string? refreshToken = null,
            string? failureReason = null,
            Dictionary<string, object>? claims = null)
        {
            IsSuccess = isSuccess;
            UserId = userId;
            Email = email;
            DisplayName = displayName;
            Roles = (roles ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            FailureReason = failureReason;
            Claims = claims ?? new Dictionary<string, object>();
        }

        public static AuthenticationResult Success(
            UserId userId,
            Email email,
            string displayName,
            IEnumerable<string> roles,
            string? refreshToken = null,
            string? accessToken = null,
            Dictionary<string, object>? additionalClaims = null)
        {
            return new AuthenticationResult(
                true,
                userId,
                email,
                displayName,
                roles,
                accessToken,
                refreshToken,
                claims: additionalClaims);
        }

        public static AuthenticationResult Failure(string reason)
        {
            return new AuthenticationResult(false, failureReason: reason);
        }

        public static AuthenticationResult AccountLocked(DateTime? lockedUntil = null)
        {
            var reason = lockedUntil.HasValue
                ? $"Account is locked until {lockedUntil:yyyy-MM-dd HH:mm:ss} UTC"
                : "Account is locked";

            return new AuthenticationResult(false, failureReason: reason);
        }

        public static AuthenticationResult InvalidCredentials()
        {
            return new AuthenticationResult(false, failureReason: "Invalid email or password");
        }

        public static AuthenticationResult TwoFactorRequired()
        {
            return new AuthenticationResult(false, failureReason: "Two-factor authentication required");
        }

        public AuthenticationResult WithAccessToken(string accessToken)
        {
            return new AuthenticationResult(
                IsSuccess,
                UserId,
                Email,
                DisplayName,
                Roles,
                accessToken,
                RefreshToken,
                FailureReason,
                Claims);
        }

        public AuthenticationResult WithClaim(string key, object value)
        {
            var newClaims = new Dictionary<string, object>(Claims)
            {
                [key] = value
            };

            return new AuthenticationResult(
                IsSuccess,
                UserId,
                Email,
                DisplayName,
                Roles,
                AccessToken,
                RefreshToken,
                FailureReason,
                newClaims);
        }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return IsSuccess;
            yield return UserId;
            yield return Email;
            yield return DisplayName;
            yield return string.Join(",", Roles);
            yield return AccessToken;
            yield return RefreshToken;
            yield return FailureReason;
        }
    }
}
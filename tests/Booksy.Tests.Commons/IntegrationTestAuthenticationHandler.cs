// ========================================
// Booksy.Tests.Common/Authentication/IntegrationTestAuthenticationHandler.cs
// ========================================
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;



    public class IntegrationTestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly TestUserContext _userContext;

        public IntegrationTestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            TestUserContext userContext)
            : base(options, logger, encoder, clock)
        {
            _userContext = userContext;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // If no test user is set, return NoResult
            if (!_userContext.HasUser)
            {
                Logger.LogWarning("No test user set in TestUserContext");
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var testUser = _userContext.CurrentUser!;

            // Generate claims from test user and authenticate directly from them — no token to
            // mint or decode. This used to also build and sign a JWT and stash it on the request's
            // Authorization header (docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 6), but nothing
            // ever consumed it: this handler already returns the AuthenticateResult straight from
            // the ClaimsPrincipal below, and ASP.NET Core's auth middleware uses that result, not a
            // re-read of the request header, once a handler has returned success.
            var claims = testUser.ToClaims().ToArray();
            var identity = new ClaimsIdentity(claims, "IntegrationTest");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "IntegrationTest");

            Logger.LogInformation("Authenticated test user: {Email} with role: {Role}",
                testUser.Email, testUser.Role);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
}


    /// <summary>
    /// Shared context for managing test user authentication state
    /// Used by both AuthenticationHelper and IntegrationTestAuthenticationHandler
    /// </summary>
    public class TestUserContext
    {
        private TestUser? _currentUser;
        private readonly object _lock = new();

        public TestUser? CurrentUser
        {
            get
            {
                lock (_lock)
                {
                    return _currentUser;
                }
            }
            set
            {
                lock (_lock)
                {
                    _currentUser = value;
                }
            }
        }

        public void SetUser(TestUser user)
        {
            CurrentUser = user;
        }

        public void ClearUser()
        {
            CurrentUser = null;
        }

        public bool HasUser => CurrentUser != null;
    }

    /// <summary>
    /// Represents a test user with claims
    /// </summary>
    public class TestUser
    {
        public string UserId { get; set; } = Guid.NewGuid().ToString();
        public string Email { get; set; } = "test@example.com";
        public string Name { get; set; } = "Test User";
        public string Role { get; set; } = "Customer";
        public Dictionary<string, string> AdditionalClaims { get; set; } = new();

        public static TestUser Customer(string email = "customer@test.com",Guid? userId = null) => new()
        {
            UserId = userId == null ? Guid.NewGuid().ToString() : userId.ToString(),
            Email = email,
            Name = email.Split('@')[0],
            Role = "Customer"
        };

        public static TestUser Provider(string email = "provider@test.com", string providerId = null) => new()
        {
            UserId = Guid.NewGuid().ToString(),
            Email = email,
            Name = email.Split('@')[0],
            Role = "Provider",
            AdditionalClaims = new Dictionary<string, string>
            {
                { "providerId", providerId ?? Guid.NewGuid().ToString() },
                { "user_type", "Provider" }
            }
        };

        /// <summary>
        /// An administrator. The application does not have one single name for that role: the
        /// <c>[Authorize(Roles = ...)]</c> attributes ask for "Admin" (payouts, payments, bulk
        /// notifications, UserManagement customers), the authorization policies in
        /// <c>PolicyAuthorizationExtensions</c> ask for "Administrator"/"SysAdmin", and controller
        /// code checks all three. A test admin therefore carries every name, so "authenticate as
        /// admin" means an actual administrator whichever spelling the endpoint happens to use.
        /// The vocabulary itself is production debt (FOLLOW-UPS #46), not something a test decides.
        /// </summary>
        public static TestUser Admin(string email = "admin@test.com") => new()
        {
            UserId = Guid.NewGuid().ToString(),
            Email = email,
            Name = email.Split('@')[0],
            Role = "Admin,Administrator,SysAdmin",
            AdditionalClaims = new Dictionary<string, string>
            {
                { "isAdmin", "true" },
                { "permissions", "all" }
            }
        };

        /// <summary>
        /// Convert TestUser to Claims for authentication
        /// </summary>
        public IEnumerable<Claim> ToClaims()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, UserId),
                new Claim(ClaimTypes.Email, Email),
                new Claim(ClaimTypes.Name, Name),
                // NO "userId" claim: the production JWT does not issue one, and minting it here
                // let controllers that read only "sub"/"userId" pass their tests while returning
                // 403 to every real caller (measured on production, 2026-09-20).
                new Claim("email", Email)
            };

            // One claim per role: a real token carries a role claim per granted role, and
            // Role is allowed to name several (see Admin()).
            foreach (var role in Role.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add additional claims
            foreach (var claim in AdditionalClaims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }

            return claims;
        }
    }

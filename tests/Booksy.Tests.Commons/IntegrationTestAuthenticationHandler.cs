// ========================================
// Booksy.Tests.Common/Authentication/IntegrationTestAuthenticationHandler.cs
// ========================================
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;



    public class IntegrationTestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly TestUserContext _userContext;
        private readonly IConfiguration _configuration;

        public IntegrationTestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            TestUserContext userContext,
            IConfiguration configuration)
            : base(options, logger, encoder, clock)
        {
            _userContext = userContext;
            _configuration = configuration;
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

            // Get JWT settings from configuration or use defaults
            var jwtSecret = _configuration["Jwt:SecretKey"] ?? "ThisIsATestSecretKeyForDevelopmentOnly12345678";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "Booksy.Test";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "Booksy.Test.Api";

            // Generate claims from test user
            var claims = testUser.ToClaims().ToArray();

            // Create JWT token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            // Create authentication ticket
            var identity = new ClaimsIdentity(claims, "IntegrationTest");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "IntegrationTest");

            // Set authorization header for downstream handlers
            Request.Headers.Authorization = $"Bearer {jwtToken}";

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

        public static TestUser Admin(string email = "admin@test.com") => new()
        {
            UserId = Guid.NewGuid().ToString(),
            Email = email,
            Name = email.Split('@')[0],
            Role = "Administrator",
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
                new Claim(ClaimTypes.Role, Role),
                new Claim("userId", UserId),
                new Claim("email", Email)
            };

            // Add additional claims
            foreach (var claim in AdditionalClaims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }

            return claims;
        }
    }

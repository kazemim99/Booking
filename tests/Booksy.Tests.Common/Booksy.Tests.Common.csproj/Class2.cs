// ========================================
// Booksy.Tests.Common/Authentication/IntegrationTestAuthenticationHandler.cs
// ========================================
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Booksy.Tests.Common.Authentication;

/// <summary>
/// Custom authentication handler for integration tests
/// Bypasses real authentication and uses claims from header
/// </summary>
public class IntegrationTestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "IntegrationTest";
    public const string UserIdHeader = "X-Test-UserId";
    public const string UserEmailHeader = "X-Test-UserEmail";
    public const string UserRolesHeader = "X-Test-UserRoles";
    public const string ProviderIdHeader = "X-Test-ProviderId";

    public IntegrationTestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if test headers are present
        if (!Request.Headers.ContainsKey(UserIdHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            var userId = Request.Headers[UserIdHeader].ToString();
            var email = Request.Headers[UserEmailHeader].ToString();
            var roles = Request.Headers[UserRolesHeader].ToString();
            var providerId = Request.Headers.ContainsKey(ProviderIdHeader)
                ? Request.Headers[ProviderIdHeader].ToString()
                : null;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim("sub", userId), // Subject claim (standard JWT)
                new Claim("email", email)
            };

            // Add roles
            if (!string.IsNullOrEmpty(roles))
            {
                foreach (var role in roles.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
                    claims.Add(new Claim("role", role.Trim())); // Alternative role claim
                }
            }

            // Add provider ID if present
            if (!string.IsNullOrEmpty(providerId))
            {
                claims.Add(new Claim("provider_id", providerId));
                claims.Add(new Claim("ProviderId", providerId));
            }

            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            return Task.FromResult(AuthenticateResult.Fail($"Authentication failed: {ex.Message}"));
        }
    }
}



/// <summary>
/// Extension methods for configuring test authentication
/// </summary>
public static class IntegrationTestAuthenticationExtensions
{
    /// <summary>
    /// Adds integration test authentication scheme
    /// </summary>
    public static IServiceCollection AddIntegrationTestAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = IntegrationTestAuthenticationHandler.AuthenticationScheme;
            options.DefaultChallengeScheme = IntegrationTestAuthenticationHandler.AuthenticationScheme;
        })
        .AddScheme<AuthenticationSchemeOptions, IntegrationTestAuthenticationHandler>(
            IntegrationTestAuthenticationHandler.AuthenticationScheme,
            options => { });

        return services;
    }
}

/// <summary>
/// Context for test authentication containing user claims
/// </summary>
public class TestAuthenticationContext
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string? ProviderId { get; set; }

    public static TestAuthenticationContext CreateUser(string userId, string email)
    {
        return new TestAuthenticationContext
        {
            UserId = userId,
            Email = email,
            Roles = new List<string> { "User" }
        };
    }

    public static TestAuthenticationContext CreateProvider(string providerId, string email)
    {
        return new TestAuthenticationContext
        {
            UserId = Guid.NewGuid().ToString(),
            Email = email,
            ProviderId = providerId,
            Roles = new List<string> { "Provider" }
        };
    }

    public static TestAuthenticationContext CreateAdmin(string userId, string email)
    {
        return new TestAuthenticationContext
        {
            UserId = userId,
            Email = email,
            Roles = new List<string> { "Admin" }
        };
    }

    public TestAuthenticationContext WithRole(string role)
    {
        if (!Roles.Contains(role))
        {
            Roles.Add(role);
        }
        return this;
    }

    public TestAuthenticationContext WithRoles(params string[] roles)
    {
        foreach (var role in roles)
        {
            WithRole(role);
        }
        return this;
    }

    public Dictionary<string, string> ToHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            [IntegrationTestAuthenticationHandler.UserIdHeader] = UserId,
            [IntegrationTestAuthenticationHandler.UserEmailHeader] = Email,
            [IntegrationTestAuthenticationHandler.UserRolesHeader] = string.Join(",", Roles)
        };

        if (!string.IsNullOrEmpty(ProviderId))
        {
            headers[IntegrationTestAuthenticationHandler.ProviderIdHeader] = ProviderId;
        }

        return headers;
    }
}
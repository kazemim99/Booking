// ========================================
// Middleware/ClientRateLimitResolver.cs
// Resolves client identity for rate limiting based on user authentication level
// ========================================

using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Booksy.API.Middleware;

/// <summary>
/// Custom client rate limit resolver that identifies users as anonymous, authenticated, or admin
/// </summary>
public class ClientRateLimitResolver : IClientResolveContributor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientRateLimitResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string> ResolveClientAsync(HttpContext httpContext)
    {
        var clientId = ResolveClientId(httpContext);
        return Task.FromResult(clientId);
    }

    private string ResolveClientId(HttpContext httpContext)
    {
        var user = httpContext.User;

        // Check if user is authenticated
        if (user?.Identity?.IsAuthenticated == true)
        {
            // Check if user has admin role
            if (user.IsInRole("Admin") || user.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Admin"))
            {
                return "admin";
            }

            // Check for admin claims with different formats
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var customRoles = user.FindAll("role").Select(c => c.Value).ToList();

            if (roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)) ||
                customRoles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
            {
                return "admin";
            }

            // One bucket PER USER, not one bucket for everybody signed in. The previous constant
            // "authenticated" meant a single 1000-per-minute allowance shared by the entire user
            // base: one busy client could throttle everyone else, and no individual account could
            // ever be limited.
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("userId")
                ?? "unknown";

            return $"user:{userId}";
        }

        // Likewise per caller, not one bucket for the whole internet. "anonymous" as a constant was
        // both useless against a single abuser (they shared the allowance with everyone) and a
        // shared-fate denial of service (they could spend everyone else's).
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ip}";
    }
}

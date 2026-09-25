// ========================================
// DependencyInjection/SecurityExtensions.cs
// ========================================
using AsanRezerve.Infrastructure.Security.Authentication;
using AsanRezerve.Infrastructure.Security.Authorization;
using AsanRezerve.Infrastructure.Security.Encryption;
using AsanRezerve.Infrastructure.Security.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AsanRezerve.Infrastructure.Security;

/// <summary>
/// Extension methods for security configuration
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Adds security services
    /// </summary>
    public static IServiceCollection AddSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add authentication
        services.AddJwtAuthentication(configuration);

        // Add authorization
        services.AddPolicyAuthorization();

        // Add rate limiting
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        services.AddSingleton<IRateLimiter, SlidingWindowRateLimiter>();

        // Add encryption
        services.AddDataProtection("AsanRezerve");
        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        return services;
    }
}
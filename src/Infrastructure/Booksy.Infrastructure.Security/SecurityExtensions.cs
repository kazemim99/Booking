// ========================================
// DependencyInjection/SecurityExtensions.cs
// ========================================
using Booksy.Infrastructure.Security.Authentication;
using Booksy.Infrastructure.Security.Authorization;
using Booksy.Infrastructure.Security.Encryption;
using Booksy.Infrastructure.Security.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.Infrastructure.Security;

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
        //services.AddJwtAuthentication(configuration);

        // Add authorization
        services.AddPolicyAuthorization();

        // Add rate limiting
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        services.AddSingleton<IRateLimiter, SlidingWindowRateLimiter>();

        // Add encryption
        services.AddDataProtection("Booksy");
        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        return services;
    }
}
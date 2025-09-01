// ========================================
// Encryption/DataProtectionExtensions.cs
// ========================================
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.Infrastructure.Security.Encryption;

/// <summary>
/// Extension methods for data protection configuration
/// </summary>
public static class DataProtectionExtensions
{
    /// <summary>
    /// Adds data protection services
    /// </summary>
    public static IServiceCollection AddDataProtection(
        this IServiceCollection services,
        string applicationName,
        string? keyStoragePath = null)
    {
        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName(applicationName);

        if (!string.IsNullOrEmpty(keyStoragePath))
        {
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keyStoragePath));
        }

        // In production, you might want to use Azure Key Vault or AWS KMS
        // dataProtectionBuilder.ProtectKeysWithAzureKeyVault(...);

        return services;
    }
}
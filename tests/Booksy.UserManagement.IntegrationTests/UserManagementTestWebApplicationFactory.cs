// ========================================
// Booksy.UserManagement.IntegrationTests/UserManagementTestWebApplicationFactory.cs
// ========================================
using Booksy.Tests.Commons;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.UserManagement.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for User Management integration tests
/// Inherits from generic TestWebApplicationFactory and can add UserManagement-specific configuration
/// </summary>
public class UserManagementTestWebApplicationFactory<TStartup>
    : TestWebApplicationFactory<TStartup, UserManagementDbContext>
    where TStartup : class
{
    public UserManagementTestWebApplicationFactory()
        : base("UserManagement")
    {
    }

    /// <summary>
    /// Configure UserManagement-specific test services
    /// </summary>
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        // Add UserManagement-specific test service replacements here
        // Example: Replace SMS verification service with fake
        // services.RemoveAll<ISmsVerificationService>();
        // services.AddSingleton<ISmsVerificationService, FakeSmsVerificationService>();

        // Example: Replace email service with fake
        // services.RemoveAll<IEmailService>();
        // services.AddSingleton<IEmailService, FakeEmailService>();
    }
}

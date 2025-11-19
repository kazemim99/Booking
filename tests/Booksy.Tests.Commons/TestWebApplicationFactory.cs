// ========================================
// Booksy.Tests.Commons/TestWebApplicationFactory.cs
// ========================================
using Booksy.Tests.Common.Fixtures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Booksy.Tests.Commons;

/// <summary>
/// Generic WebApplicationFactory for integration tests across all bounded contexts
/// Configures test services and uses Testcontainers PostgreSQL
/// </summary>
/// <typeparam name="TStartup">The Startup class from the API project</typeparam>
/// <typeparam name="TDbContext">The DbContext for the bounded context</typeparam>
public class TestWebApplicationFactory<TStartup, TDbContext>
    : WebApplicationFactory<TStartup>
    where TStartup : class
    where TDbContext : DbContext
{
    private readonly PostgresTestContainerFixture _postgresFixture;
    private readonly string _contextName;

    public TestWebApplicationFactory(string contextName)
    {
        _contextName = contextName;
        _postgresFixture = new PostgresTestContainerFixture();
        _postgresFixture.InitializeAsync().GetAwaiter().GetResult();
    }

    public string ConnectionString => _postgresFixture.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test-specific configuration
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                [$"ConnectionStrings:{_contextName}"] = _postgresFixture.ConnectionString,
                ["ConnectionStrings:DefaultConnection"] = _postgresFixture.ConnectionString,
                ["DatabaseSettings:EnableSensitiveDataLogging"] = "true"
            }!);
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with Testcontainers connection string
            services.AddDbContext<TDbContext>(options =>
            {
                options.UseNpgsql(_postgresFixture.ConnectionString);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Replace authentication with test authentication handler
            services.RemoveAll(typeof(IAuthenticationService));
            services.RemoveAll(typeof(IAuthenticationSchemeProvider));

            // Add test authentication
            services.AddSingleton<TestUserContext>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "IntegrationTest";
                options.DefaultChallengeScheme = "IntegrationTest";
                options.DefaultScheme = "IntegrationTest";
            })
            .AddScheme<AuthenticationSchemeOptions, IntegrationTestAuthenticationHandler>(
                "IntegrationTest",
                options => { });

            // Allow derived factories to add custom service configuration
            ConfigureTestServices(services);

            // Build service provider and ensure database is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
            dbContext.Database.EnsureCreated();
        });

        builder.UseEnvironment("Test");
    }

    /// <summary>
    /// Override this method in derived classes to configure test-specific services
    /// Example: Replace email service with fake, configure mocks, etc.
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Override in derived classes if needed
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _postgresFixture.DisposeAsync().GetAwaiter().GetResult();
        }
        base.Dispose(disposing);
    }
}

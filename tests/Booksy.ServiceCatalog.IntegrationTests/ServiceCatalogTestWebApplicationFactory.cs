// ========================================
// Booksy.ServiceCatalog.IntegrationTests/Infrastructure/ServiceCatalogTestWebApplicationFactory.cs
// ========================================
using Booksy.Tests.Common.Fixtures;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication;

namespace Booksy.ServiceCatalog.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for Service Catalog integration tests
/// Configures test services and uses Testcontainers PostgreSQL
/// This class lives in the IntegrationTests project, NOT in Tests.Common
/// </summary>
public class ServiceCatalogTestWebApplicationFactory<TStartup>
    : WebApplicationFactory<TStartup>
    where TStartup : class
{
    private readonly PostgresTestContainerFixture _postgresFixture;

    public ServiceCatalogTestWebApplicationFactory()
    {
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
                ["ConnectionStrings:ServiceCatalog"] = _postgresFixture.ConnectionString,
                ["ConnectionStrings:DefaultConnection"] = _postgresFixture.ConnectionString,
                ["DatabaseSettings:EnableSensitiveDataLogging"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ServiceCatalogDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with Testcontainers connection string
            services.AddDbContext<ServiceCatalogDbContext>(options =>
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

            // Replace production services with test doubles if needed
            // Example: Replace email service with fake
            // services.RemoveAll<IEmailService>();
            // services.AddSingleton<IEmailService, FakeEmailService>();

            // Build service provider and ensure database is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            dbContext.Database.EnsureCreated();
        });

        builder.UseEnvironment("Test");
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
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
        // UseSetting, not just ConfigureAppConfiguration.
        //
        // ConfigureAppConfiguration is applied while the host is being built — AFTER the entry point's
        // top-level statements have already registered services. Anything that resolves a connection
        // string from IConfiguration *during registration* therefore never sees the override and keeps
        // the value from the Host's own appsettings.json, which points at the developer's local Postgres
        // (Port=54321). CAP's storage initializer is one such consumer, but it is not the only one: this
        // was the single largest cause of failures when the suite was retargeted at Booksy.Host, showing
        // up as "Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:54321" across whole test classes.
        //
        // UseSetting writes into the host configuration before the application builder reads it, so
        // registration-time consumers see the container's connection string too. It is per-builder rather
        // than process-wide, so each factory keeps its own database and test isolation is unaffected.
        builder.UseSetting($"ConnectionStrings:{_contextName}", _postgresFixture.ConnectionString);
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgresFixture.ConnectionString);

        // Everything else that used to be a UseSetting call here — the in-memory cache, the lazy Redis
        // connection string, the raised OTP abuse limits, rate limiting off, and Database:SeedOnStartup
        // — now lives in the host's own appsettings.Testing.json, loaded because of UseEnvironment
        // ("Testing") below. A file is visible to anyone reading the host; a stack of UseSetting calls in
        // a test base class is not. Only the connection string stays here: it is per-container, and its
        // consumers (CAP's storage initializer among them) read it during service registration, which is
        // before ConfigureAppConfiguration runs.

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                [$"ConnectionStrings:{_contextName}"] = _postgresFixture.ConnectionString,
                ["ConnectionStrings:DefaultConnection"] = _postgresFixture.ConnectionString,
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

            // The host registers a Redis-backed IDistributedCache
            // unconditionally (Program.cs); swap it for the in-memory one so
            // tests never depend on a live Redis.
            services.RemoveAll(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache));
            services.AddDistributedMemoryCache();

            // Allow derived factories to add custom service configuration
            ConfigureTestServices(services);

            // NO Migrate() here, and no BuildServiceProvider() to run it with. The host migrates both
            // contexts during startup (Program.cs → MigrateAndSeedDatabaseAsync / InitializeDatabaseAsync),
            // so this was a second, redundant migration of the same fresh database — and it built a whole
            // throwaway container (ASP0000) to do it, giving those singletons a second, parallel lifetime.
        });

        // "Testing", not "Test": the name selects appsettings.Testing.json (see above), and it must NOT
        // contain the substring the seeders used to gate on.
        builder.UseEnvironment("Testing");
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

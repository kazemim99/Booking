// ========================================
// Booksy.Tests.Commons/TestWebApplicationFactory.cs
// ========================================
using Booksy.Tests.Common.Fixtures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly DatabaseReset _databaseReset;

    public TestWebApplicationFactory(string contextName)
    {
        _contextName = contextName;
        _postgresFixture = new PostgresTestContainerFixture();
        _postgresFixture.InitializeAsync().GetAwaiter().GetResult();
        _databaseReset = new DatabaseReset(_postgresFixture.ConnectionString);
    }

    public string ConnectionString => _postgresFixture.ConnectionString;

    /// <summary>Exposed for the fixture self-test (docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 1, S2).</summary>
    public DatabaseReset DatabaseReset => _databaseReset;

    /// <summary>
    /// Everything a test needs cleared before the next one runs, now that a factory (and the state
    /// behind it) is shared rather than rebuilt per class: the database, the in-memory aggregate
    /// cache, the resettable distributed cache, and every registered <see cref="IResettableFake"/>.
    /// Called from <c>IntegrationTestBase.InitializeAsync</c>, replacing the no-op
    /// <c>CleanDatabaseAsync</c> the base class used to declare.
    /// </summary>
    public async Task ResetStateAsync(CancellationToken cancellationToken = default)
    {
        await _databaseReset.ResetAsync(cancellationToken);

        // Both are process-wide singletons; resolving them from the root provider is correct and
        // matches how ASP.NET Core itself resolves singleton services.
        if (Services.GetService<IMemoryCache>() is MemoryCache memoryCache)
        {
            memoryCache.Clear();
        }

        if (Services.GetService<IDistributedCache>() is ResettableDistributedCache resettableCache)
        {
            resettableCache.Reset();
        }

        foreach (var fake in Services.GetServices<IResettableFake>())
        {
            fake.Reset();
        }
    }

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
            // No DbContext override here any more. It used to remove the production registration
            // and re-add a bare UseNpgsql(connectionString) — with no MigrationsHistoryTable, no
            // warning configuration, unconditional EnableSensitiveDataLogging/EnableDetailedErrors —
            // which is the one place a test's database differed from production's. It is also
            // redundant: the UseSetting calls above already point ConnectionStrings:{context} and
            // :DefaultConnection at the container, and every AddXInfrastructure registration reads
            // its connection string from configuration, so the PRODUCTION registration already
            // targets this container. Sensitive-data logging is controlled the same way production
            // controls it: DatabaseSettings:EnableSensitiveDataLogging in configuration
            // (appsettings.Testing.json sets it false — see docs/TEST_ARCHITECTURE_AUDIT.md Phase 2,
            // the flag was the largest source of the console flood Phase 1 left behind).

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

            // The host registers a Redis-backed IDistributedCache unconditionally (Program.cs); swap
            // it for a cache tests never depend on a live Redis for, AND that ResetStateAsync can
            // empty between tests — plain AddDistributedMemoryCache() has no such capability.
            services.RemoveAll(typeof(IDistributedCache));
            services.AddSingleton<ResettableDistributedCache>();
            services.AddSingleton<IDistributedCache>(sp => sp.GetRequiredService<ResettableDistributedCache>());

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

using Booksy.Tests.Common.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Booksy.Host.CompositionTests;

/// <summary>
/// Boots the real <c>Booksy.Host</c> — the same top-level Program the container runs — against
/// a throwaway PostgreSQL Testcontainer, so assertions are made about the actual composed
/// service graph rather than a hand-built one.
///
/// <para>The Host migrates BOTH contexts during startup, so a live database is unavoidable here.
/// The environment is <c>Testing</c>, which loads the Host's <c>appsettings.Testing.json</c>:
/// quiet logging, no Redis, and <c>Database:SeedOnStartup=false</c>, so startup is migrations
/// alone. It used to be <c>Staging</c> for the last of those reasons only — seeding was gated on
/// the environment's NAME containing "Test", so the one name this suite could not use was the one
/// that described it.</para>
/// </summary>
// TEntryPoint is only used to locate the assembly holding the entry point, and both
// Booksy.Host and Booksy.UserManagement.API declare a global `Program` (CS0433 if named
// directly). Any type from Booksy.Host serves; the adapter under test is the clearest.
public sealed class HostCompositionFactory
    : WebApplicationFactory<Booksy.Host.Composition.InProcessProviderInfoService>, IAsyncLifetime
{
    private readonly PostgresTestContainerFixture _postgres = new();

    public async Task InitializeAsync() => await _postgres.InitializeAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Only the connection string: everything else this factory used to set (lazy Redis, quiet
        // logging, no seeding) is in the Host's appsettings.Testing.json. UseSetting as well as
        // ConfigureAppConfiguration because registration-time consumers — CAP's storage initializer
        // among them — read the connection string before the app configuration is built.
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.ConnectionString);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Point both contexts at the container instead of the developer's local
                // Postgres, so a test run can never migrate or seed a real database.
                ["ConnectionStrings:DefaultConnection"] = _postgres.ConnectionString,
            });
        });
    }
}

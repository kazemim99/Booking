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
/// <para>The Host migrates BOTH contexts during startup, so a live database is unavoidable
/// here. The environment is set to Staging deliberately: seeding is gated on
/// <c>IsDevelopment() || EnvironmentName.Contains("Test")</c>, and this suite only cares about
/// how services are wired, so skipping the seed keeps startup to migrations alone.</para>
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
        builder.UseEnvironment("Staging");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Point both contexts at the container instead of the developer's local
                // Postgres, so a test run can never migrate or seed a real database.
                ["ConnectionStrings:DefaultConnection"] = _postgres.ConnectionString,

                // No Redis in this suite. Both cache connection strings keep
                // abortConnect=false so StackExchange.Redis stays lazy and a missing
                // server cannot fail host startup — nothing here touches the cache.
                ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false",
                ["Cache:RedisConnectionString"] = "localhost:6379,abortConnect=false",
            });
        });
    }
}

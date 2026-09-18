using Booksy.Core.Application.Services.Notifications;
using Booksy.Host.IntegrationTests.Infrastructure.Fakes;
using Booksy.Infrastructure.External.Payment;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.Tests.Common.Fixtures;
using Booksy.Tests.Commons;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Booksy.Host.IntegrationTests.Infrastructure;

/// <summary>
/// The one <c>WebApplicationFactory</c> every integration test in this project boots against —
/// ServiceCatalog and UserManagement alike (docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 4). It
/// replaces <c>ServiceCatalogTestWebApplicationFactory&lt;TStartup&gt;</c> and
/// <c>UserManagementTestWebApplicationFactory&lt;TStartup&gt;</c>, which were two closed generic
/// types over the same underlying <c>TestWebApplicationFactory&lt;TStartup,TDbContext&gt;</c> and so
/// could never share ONE booted host between the two suites, only within each. Composition's own
/// <see cref="Booksy.Host.IntegrationTests.Composition.HostCompositionFactory"/> stays separate and
/// deliberately unfaked — its tests assert on the real, undecorated production DI graph.
///
/// <para>Non-generic over <c>TDbContext</c>, unlike its two predecessors: nothing in this class ever
/// used the type parameter for anything but a compile-time marker (the per-test-base
/// <c>DbContext</c> property is resolved by <see cref="IntegrationTestBase{TDbContext}"/> instead,
/// from this factory's own DI scope). Not generic over <c>TStartup</c> either — there is exactly one
/// entry point in this repository now, <c>Startup</c> (the alias GlobalUsing.cs gives
/// <c>Booksy.Host.HostEntryPoint</c>).</para>
///
/// <para>Only one connection string is set (<c>DefaultConnection</c>), not a per-context one: with
/// ServiceCatalog and UserManagement sharing this single factory and database, and neither context's
/// own connection-string key (<c>ConnectionStrings:ServiceCatalog</c> /
/// <c>:UserManagement</c>) ever configured, both fall back to <c>DefaultConnection</c> by design
/// (see each context's own <c>AddXInfrastructure</c>) — the per-context key this class used to set
/// bought nothing once the two suites stopped needing separate databases.</para>
/// </summary>
public sealed class BooksyHostFactory : WebApplicationFactory<Startup>
{
    private readonly PostgresTestContainerFixture _postgresFixture = new();
    private readonly DatabaseReset _databaseReset;

    public BooksyHostFactory()
    {
        _postgresFixture.InitializeAsync().GetAwaiter().GetResult();
        _databaseReset = new DatabaseReset(_postgresFixture.ConnectionString);
    }

    public string ConnectionString => _postgresFixture.ConnectionString;

    /// <summary>Exposed for the fixture self-test (docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 1, S2).</summary>
    public DatabaseReset DatabaseReset => _databaseReset;

    /// <summary>
    /// Everything a test needs cleared before the next one runs, now that this factory (and the
    /// state behind it) is shared across the whole suite rather than rebuilt per class: the
    /// database, the in-memory aggregate cache, the resettable distributed cache, and every
    /// registered <see cref="IResettableFake"/>. Called from
    /// <see cref="IntegrationTestBase{TDbContext}.InitializeAsync"/>.
    /// </summary>
    public async Task ResetStateAsync(CancellationToken cancellationToken = default)
    {
        // Start the host (and so run its migrations) before the reset discovers tables. Some classes
        // reset from InitializeAsync without having created a client; when one of them ran first, the
        // reset found no tables, cached that, and silently truncated nothing for the rest of the run.
        _ = Services;

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
        // UseSetting, not just ConfigureAppConfiguration — see the historical note in git blame for
        // why (registration-time consumers, CAP's storage initializer among them, read
        // configuration before ConfigureAppConfiguration's callback ever runs).
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgresFixture.ConnectionString);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgresFixture.ConnectionString,
            }!);
        });

        builder.ConfigureServices(services =>
        {
            // Replace authentication with the test authentication handler.
            services.RemoveAll(typeof(IAuthenticationService));
            services.RemoveAll(typeof(IAuthenticationSchemeProvider));
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

            // The host registers a Redis-backed IDistributedCache unconditionally; swap it for one
            // ResetStateAsync can empty between tests.
            services.RemoveAll(typeof(IDistributedCache));
            services.AddSingleton<ResettableDistributedCache>();
            services.AddSingleton<IDistributedCache>(sp => sp.GetRequiredService<ResettableDistributedCache>());

            // ---- ServiceCatalog fakes ----
            // The real registration resolves IPaymentGateway through the gateway factory to the
            // configured provider (ZarinPal by default), whose placeholder merchant id makes every
            // payment command fail with the gateway's own error (FOLLOW-UPS #31). Tests exercise
            // the application's payment behaviour, not the gateway's.
            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway, FakePaymentGateway>();

            // Every notification channel talks to the outside world (SMTP, an SMS gateway, Firebase,
            // SignalR) and none of them can reach it from the test sandbox. Fakes here let a test
            // assert the notification lifecycle (queue -> send -> delivered) through the real
            // pipeline instead of measuring the sandbox's lack of outbound network.
            services.RemoveAll<IEmailNotificationService>();
            services.AddSingleton<IEmailNotificationService, FakeEmailNotificationService>();
            services.AddSingleton<IResettableFake>(sp => (IResettableFake)sp.GetRequiredService<IEmailNotificationService>());

            // ISmsNotificationService: ONE fake for both bounded contexts, because ONE registration
            // wins process-wide regardless of which context's AddXInfrastructure calls
            // AddSmsNotificationService first. This is UserManagement's capturing fake (it exposes
            // LastMessageTo, which the OTP/phone-change tests need to recover the code that was
            // "sent") rather than ServiceCatalog's older non-capturing FakeSmsGateway — the merge
            // keeps the more capable of the two rather than either arbitrarily or both.
            services.RemoveAll<ISmsNotificationService>();
            services.AddSingleton<ISmsNotificationService, FakeSmsNotificationService>();
            services.AddSingleton<IResettableFake>(sp => (IResettableFake)sp.GetRequiredService<ISmsNotificationService>());

            services.RemoveAll<IPushNotificationService>();
            services.AddSingleton<IPushNotificationService, FakePushNotificationService>();
            services.RemoveAll<IInAppNotificationService>();
            services.AddSingleton<IInAppNotificationService, FakeInAppNotificationService>();

            // NO Migrate() here, and no BuildServiceProvider() to run it with — the host migrates
            // both contexts during startup. No DbContext override either: the UseSetting/
            // ConfigureAppConfiguration calls above already route the PRODUCTION registration at
            // this container.
        });

        // "Testing", not "Test": the name selects appsettings.Testing.json, and it must NOT contain
        // the substring the old seed gate used to match on.
        builder.UseEnvironment("Testing");
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

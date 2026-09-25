using AsanRezerve.Infrastructure.Observability.Logging;
using AsanRezerve.Infrastructure.Observability.Logging.Levels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;

namespace AsanRezerve.Infrastructure.Observability.UnitTests;

/// <summary>
/// The host's logging pipeline over the host's own appsettings.json, with console and file switched off and a
/// collecting sink in their place.
/// </summary>
internal sealed class LoggingHarness : IDisposable
{
    public CollectingSink Sink { get; } = new();

    public ServiceProvider Services { get; }

    public IConfiguration Configuration { get; }

    public LoggingHarness(IDictionary<string, string?>? overrides = null, bool hostSettings = true, TimeProvider? time = null, ILogLevelOverrideStore? store = null)
    {
        var builder = new ConfigurationBuilder();
        if (hostSettings) builder.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "HostSettings", "appsettings.json"));
        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Observability:Logging:Console:Enabled"] = "false",
            ["Observability:Logging:File:Enabled"] = "false",
        });
        if (overrides is not null) builder.AddInMemoryCollection(overrides);
        var services = new ServiceCollection();
        RuntimeLogLevels.Attach(builder, services);
        Configuration = builder.Build();

        services.AddSingleton(Configuration);
        services.AddSingleton<ILogEventSink>(Sink);
        services.AddSingleton(time ?? TimeProvider.System);
        if (store is not null) services.AddSingleton(store);
        services.AddLogging(logging =>
        {
            logging.AddConfiguration(Configuration.GetSection("Logging"));
            logging.AddAsanRezerveLogging(Configuration);
        });
        Services = services.BuildServiceProvider();
    }

    public ILoggerFactory Factory => Services.GetRequiredService<ILoggerFactory>();

    public LogLevelService Levels => Services.GetRequiredService<LogLevelService>();

    public void Dispose() => Services.Dispose();
}

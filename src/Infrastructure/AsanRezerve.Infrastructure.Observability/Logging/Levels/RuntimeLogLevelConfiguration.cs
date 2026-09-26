using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.Infrastructure.Observability.Logging.Levels;

/// <summary>
/// Holds the admin's log-level overrides as <c>Logging:LogLevel:&lt;category&gt;</c> keys. Added last to the host
/// configuration, so it wins over appsettings and environment variables; <see cref="Apply"/> raises a reload, on
/// which Microsoft.Extensions.Logging re-applies its filter rules to every logger that exists (design D2).
/// </summary>
public sealed class RuntimeLogLevelConfigurationProvider : ConfigurationProvider
{
    public const string Prefix = "Logging:LogLevel:";

    public void Apply(IEnumerable<KeyValuePair<string, LogLevel>> overrides)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (category, level) in overrides)
            data[Prefix + category] = level.ToString();

        Data = data;
        OnReload();
    }
}

public sealed class RuntimeLogLevelConfigurationSource : IConfigurationSource
{
    public RuntimeLogLevelConfigurationProvider Provider { get; } = new();

    public IConfigurationProvider Build(IConfigurationBuilder builder) => Provider;
}

public static class RuntimeLogLevels
{
    /// <summary>
    /// Adds the override source to <paramref name="configuration"/> (call it after every other source) and the
    /// services that manage it. The store defaults to in-memory; the database log store replaces it.
    /// </summary>
    public static void Attach(IConfigurationBuilder configuration, IServiceCollection services)
    {
        var source = new RuntimeLogLevelConfigurationSource();
        configuration.Add(source);

        services.AddSingleton(source.Provider);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ILogLevelOverrideStore, InMemoryLogLevelOverrideStore>();
        services.AddSingleton<LogLevelService>();
        services.AddHostedService<LogLevelMaintenanceService>();
    }
}

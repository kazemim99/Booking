using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.Infrastructure.Observability.Logging.Levels;

/// <summary>
/// Lists, sets, resets and expires log-level overrides (runtime-log-levels). Every change takes effect at once,
/// is persisted so it survives a restart, and is audited as a Warning <c>Log level for …</c> event naming the admin.
/// </summary>
public sealed partial class LogLevelService
{
    public const string DefaultCategory = "Default";

    /// <summary>Longest a temporary override may run.</summary>
    public static readonly TimeSpan MaxDuration = TimeSpan.FromDays(7);

    /// <summary>Offered on the admin page even when configuration has no rule for them.</summary>
    public static readonly IReadOnlyList<string> WellKnownCategories =
    [
        DefaultCategory,
        "AsanRezerve",
        "AsanRezerve.ServiceCatalog",
        "AsanRezerve.UserManagement",
        "AsanRezerve.Infrastructure",
        "AsanRezerve.API",
        "AsanRezerve.Core.Application.Behaviors",
        "AsanRezerve.Infrastructure.Observability.Diagnostics.RequestTelemetryMiddleware",
        "Microsoft",
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.EntityFrameworkCore.Database.Command",
        "System.Net.Http",
        "DotNetCore.CAP",
        "Npgsql",
    ];

    private readonly RuntimeLogLevelConfigurationProvider _provider;
    private readonly IConfiguration _configuration;
    private readonly ILogLevelOverrideStore _store;
    private readonly TimeProvider _time;
    private readonly ILogger<LogLevelService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, LogLevelOverride> _overrides = new(StringComparer.OrdinalIgnoreCase);

    public LogLevelService(
        RuntimeLogLevelConfigurationProvider provider,
        IConfiguration configuration,
        ILogLevelOverrideStore store,
        TimeProvider time,
        ILogger<LogLevelService> logger)
    {
        _provider = provider;
        _configuration = configuration;
        _store = store;
        _time = time;
        _logger = logger;
    }

    /// <summary>Every configured, well-known or overridden category, <see cref="DefaultCategory"/> first.</summary>
    public IReadOnlyList<LogLevelInfo> GetLevels()
    {
        Dictionary<string, LogLevelOverride> overrides;
        lock (_overrides) overrides = new(_overrides, StringComparer.OrdinalIgnoreCase);

        var configured = ConfiguredLevels();
        return configured.Keys
            .Concat(WellKnownCategories)
            .Concat(overrides.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(category => new LogLevelInfo(
                category,
                configured.TryGetValue(category, out var level) ? level : null,
                Effective(category, configured, overrides),
                overrides.GetValueOrDefault(category)))
            .OrderBy(info => info.Category == DefaultCategory ? 0 : 1)
            .ThenBy(info => info.Category, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<LogLevelInfo> SetAsync(
        string category, LogLevel level, TimeSpan? duration, string actor, CancellationToken cancellationToken = default)
    {
        Validate(category);
        if (!Enum.IsDefined(level)) throw new ArgumentException($"Unknown log level '{level}'.", nameof(level));
        if (duration is { } d && (d <= TimeSpan.Zero || d > MaxDuration))
            throw new ArgumentException($"A temporary override lasts between one minute and {MaxDuration.TotalDays:0} days.", nameof(duration));

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var previous = EffectiveNow(category);
            var now = _time.GetUtcNow();
            var value = new LogLevelOverride(category, level, duration is { } span ? now + span : null, actor, now);

            await _store.UpsertAsync(value, cancellationToken);
            lock (_overrides) _overrides[category] = value;
            ApplyAll();

            _logger.LogWarning(
                "Log level for {Category} changed from {PreviousLevel} to {NewLevel} by {Actor} until {ExpiresAt}",
                category, previous.ToString(), level.ToString(), actor, value.ExpiresAt?.ToString("O") ?? "reset");

            return GetLevels().Single(l => string.Equals(l.Category, category, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ResetAsync(string category, string actor, CancellationToken cancellationToken = default)
    {
        Validate(category);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            bool removed;
            lock (_overrides) removed = _overrides.Remove(category);
            if (!removed) return;

            var previous = EffectiveNow(category);
            await _store.DeleteAsync(category, cancellationToken);
            ApplyAll();

            _logger.LogWarning(
                "Log level for {Category} changed from {PreviousLevel} to {NewLevel} by {Actor} until {ExpiresAt}",
                category, previous.ToString(), EffectiveNow(category).ToString(), actor, "reset");
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Removes overrides whose time is up. Run every 30 seconds by <see cref="LogLevelMaintenanceService"/>.</summary>
    public async Task ExpireDueAsync(CancellationToken cancellationToken = default)
    {
        var now = _time.GetUtcNow();
        List<LogLevelOverride> due;
        lock (_overrides) due = _overrides.Values.Where(o => o.ExpiresAt <= now).ToList();

        foreach (var expired in due)
            await ResetAsync(expired.Category, "expiry", cancellationToken);
    }

    /// <summary>Re-applies persisted overrides at startup, dropping those that expired while the host was down.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var now = _time.GetUtcNow();
        var persisted = await _store.LoadAsync(cancellationToken);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            foreach (var value in persisted)
            {
                if (value.ExpiresAt <= now)
                {
                    await _store.DeleteAsync(value.Category, cancellationToken);
                    continue;
                }

                lock (_overrides) _overrides[value.Category] = value;
            }

            ApplyAll();
        }
        finally
        {
            _gate.Release();
        }

        if (_overrides.Count > 0)
            _logger.LogWarning("Re-applied {Count} log-level override(s) from before the restart", _overrides.Count);
    }

    private void ApplyAll()
    {
        List<KeyValuePair<string, LogLevel>> values;
        lock (_overrides) values = _overrides.Values.Select(o => KeyValuePair.Create(o.Category, o.Level)).ToList();
        _provider.Apply(values);
    }

    private LogLevel EffectiveNow(string category)
    {
        Dictionary<string, LogLevelOverride> overrides;
        lock (_overrides) overrides = new(_overrides, StringComparer.OrdinalIgnoreCase);
        return Effective(category, ConfiguredLevels(), overrides);
    }

    /// <summary>The same longest-prefix rule Microsoft.Extensions.Logging applies, over configuration + overrides.</summary>
    private static LogLevel Effective(
        string category,
        IReadOnlyDictionary<string, LogLevel> configured,
        IReadOnlyDictionary<string, LogLevelOverride> overrides)
    {
        LogLevel? Rule(string key) =>
            overrides.TryGetValue(key, out var o) ? o.Level : configured.TryGetValue(key, out var c) ? c : null;

        if (!string.Equals(category, DefaultCategory, StringComparison.OrdinalIgnoreCase))
        {
            var best = configured.Keys.Concat(overrides.Keys)
                .Where(key => !string.Equals(key, DefaultCategory, StringComparison.OrdinalIgnoreCase))
                .Where(key => category.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(key => key.Length)
                .FirstOrDefault();
            if (best is not null && Rule(best) is { } level) return level;
        }

        return Rule(DefaultCategory) ?? LogLevel.Information;
    }

    /// <summary><c>Logging:LogLevel</c> from every configuration source except the overrides.</summary>
    private Dictionary<string, LogLevel> ConfiguredLevels()
    {
        var result = new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase);
        if (_configuration is not IConfigurationRoot root) return result;

        foreach (var source in root.Providers.Where(p => !ReferenceEquals(p, _provider)))
        {
            foreach (var key in source.GetChildKeys([], "Logging:LogLevel").Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (source.TryGet(RuntimeLogLevelConfigurationProvider.Prefix + key, out var raw)
                    && Enum.TryParse<LogLevel>(raw, ignoreCase: true, out var level))
                {
                    result[key] = level;
                }
            }
        }

        return result;
    }

    private static void Validate(string category)
    {
        if (string.IsNullOrWhiteSpace(category) || category.Length > 200 || !CategoryPattern().IsMatch(category))
            throw new ArgumentException("A category is a namespace or type name: letters, digits, '_' and '.'.", nameof(category));
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_.]*$", RegexOptions.CultureInvariant)]
    private static partial Regex CategoryPattern();
}

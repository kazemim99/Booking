using Serilog.Events;

namespace AsanRezerve.Infrastructure.Observability.LogStore;

/// <summary>The <c>Observability:LogStore</c> section: the database log store the admin Logs page reads.</summary>
public sealed class LogStoreOptions
{
    public const string Section = "Observability:LogStore";

    public bool Enabled { get; set; } = true;

    /// <summary>Events below this level reach console/file/Seq but are not stored.</summary>
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

    /// <summary>How long events are kept (decision 2026-09-25: 14 days).</summary>
    public int RetentionDays { get; set; } = 14;

    /// <summary>Events waiting to be written; beyond this they are dropped (and counted), never blocking a caller.</summary>
    public int QueueCapacity { get; set; } = 10_000;

    public int BatchSize { get; set; } = 500;

    public int FlushIntervalMs { get; set; } = 2_000;
}

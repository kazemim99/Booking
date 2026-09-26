namespace AsanRezerve.Infrastructure.Observability.LogStore;

/// <summary>A stored log event (<c>observability.log_events</c>). Rows are written by <see cref="LogStoreWriter"/>.</summary>
public sealed class LogEventRecord
{
    public long Id { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Serilog level: 0 Verbose … 5 Fatal.</summary>
    public short Level { get; set; }

    /// <summary>The rendered, masked message.</summary>
    public string Message { get; set; } = string.Empty;

    public string? MessageTemplate { get; set; }

    public string? Exception { get; set; }

    public string? SourceContext { get; set; }

    public string? TraceId { get; set; }

    public string? SpanId { get; set; }

    public string? RequestPath { get; set; }

    public string? RouteTemplate { get; set; }

    public int? StatusCode { get; set; }

    public double? ElapsedMs { get; set; }

    public string? UserId { get; set; }

    /// <summary>Every other property of the event, as a JSON object.</summary>
    public string? Properties { get; set; }
}

/// <summary>A persisted log-level override (<c>observability.log_level_overrides</c>).</summary>
public sealed class LogLevelOverrideRecord
{
    public string Category { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public DateTimeOffset? ExpiresAt { get; set; }

    public string UpdatedBy { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }
}

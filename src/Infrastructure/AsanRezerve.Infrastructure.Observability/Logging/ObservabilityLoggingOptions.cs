namespace AsanRezerve.Infrastructure.Observability.Logging;

/// <summary>The <c>Observability:Logging</c> section: where log events go. What is logged is <c>Logging:LogLevel</c>.</summary>
public sealed class ObservabilityLoggingOptions
{
    public const string Section = "Observability:Logging";

    public ConsoleSinkOptions Console { get; set; } = new();

    public FileSinkOptions File { get; set; } = new();

    public SeqSinkOptions Seq { get; set; } = new();

    /// <summary>A request slower than this is logged at Warning.</summary>
    public int SlowRequestThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Events each asynchronous sink buffers before it starts dropping. Console and file are written on a background
    /// thread, never on the request's.
    /// </summary>
    public int AsyncBufferSize { get; set; } = 10_000;

    public sealed class ConsoleSinkOptions
    {
        public bool Enabled { get; set; } = true;

        public string OutputTemplate { get; set; } =
            "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {TraceId}{NewLine}{Exception}";
    }

    public sealed class FileSinkOptions
    {
        public bool Enabled { get; set; } = true;

        /// <summary>Rolling compact-JSON (CLEF) files: one event per line, machine- and AI-readable.</summary>
        public string Path { get; set; } = "logs/asanrezerve-host-.clef";

        public int FileSizeLimitMb { get; set; } = 50;

        public int RetainedFileCount { get; set; } = 7;
    }

    public sealed class SeqSinkOptions
    {
        /// <summary>Empty = no Seq. Also read from <c>Seq:ServerUrl</c>, the key the compose files set.</summary>
        public string? ServerUrl { get; set; }

        public string? ApiKey { get; set; }

        public int QueueSizeLimit { get; set; } = 10_000;
    }
}

using Microsoft.Extensions.Options;
using Serilog.Core;
using Serilog.Events;

namespace AsanRezerve.Infrastructure.Observability.LogStore;

/// <summary>The Serilog sink that hands events at or above the store's minimum level to <see cref="LogStoreWriter"/>.</summary>
public sealed class LogStoreSink(LogStoreWriter writer, IOptions<LogStoreOptions> options) : ILogEventSink
{
    private readonly LogEventLevel _minimum = options.Value.MinimumLevel;

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level >= _minimum)
            writer.TryEnqueue(logEvent);
    }
}

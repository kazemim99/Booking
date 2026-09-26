using Serilog.Core;
using Serilog.Events;

using FluentAssertions;

namespace AsanRezerve.Infrastructure.Observability.UnitTests;

/// <summary>A Serilog sink that keeps what it receives, for assertions.</summary>
internal sealed class CollectingSink : ILogEventSink
{
    private readonly List<LogEvent> _events = [];

    public IReadOnlyList<LogEvent> Events
    {
        get
        {
            lock (_events) return _events.ToList();
        }
    }

    public void Emit(LogEvent logEvent)
    {
        lock (_events) _events.Add(logEvent);
    }

    public LogEvent Single() => Events.Should().ContainSingle().Subject;
}

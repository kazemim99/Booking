using System.Diagnostics;
using System.Text.Json;
using AsanRezerve.Infrastructure.Observability.LogStore;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Events;
using Serilog.Parsing;

namespace AsanRezerve.Infrastructure.Observability.UnitTests.LogStore;

/// <summary>
/// The database log store behind the admin Logs page (log-explorer: "Log events are stored without affecting
/// requests"). Logging calls only enqueue; a background writer copies batches to Postgres. A full queue drops and
/// counts, a failing database is retried once and counted — neither ever reaches the request that logged.
/// </summary>
public sealed class LogStoreWriterTests
{
    private sealed class RecordingBatchWriter : ILogEventBatchWriter
    {
        public List<IReadOnlyList<LogEventRow>> Batches { get; } = [];

        public int FailuresLeft { get; set; }

        public Task WriteAsync(IReadOnlyList<LogEventRow> rows, CancellationToken cancellationToken)
        {
            if (FailuresLeft > 0)
            {
                FailuresLeft--;
                throw new InvalidOperationException("database down");
            }

            lock (Batches) Batches.Add(rows.ToList());
            return Task.CompletedTask;
        }

        public IReadOnlyList<LogEventRow> Rows
        {
            get
            {
                lock (Batches) return Batches.SelectMany(b => b).ToList();
            }
        }
    }

    private static LogEvent Event(string text = "hello", LogEventLevel level = LogEventLevel.Information, params (string Name, object? Value)[] properties) =>
        new(DateTimeOffset.UtcNow, level, null, new MessageTemplateParser().Parse(text),
            properties.Select(p => new LogEventProperty(p.Name, new ScalarValue(p.Value))));

    private static LogStoreWriter Writer(RecordingBatchWriter batches, int capacity = 100, int batchSize = 500) =>
        new(batches, Options.Create(new LogStoreOptions { QueueCapacity = capacity, BatchSize = batchSize, FlushIntervalMs = 50 }), TimeProvider.System);

    [Fact]
    public void A_row_carries_the_rendered_message_its_request_and_the_rest_as_json()
    {
        using var harness = new LoggingHarness();
        using var activity = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();
        var logger = harness.Factory.CreateLogger("AsanRezerve.ServiceCatalog.Test");
        using (logger.BeginScope(new Dictionary<string, object?> { ["RequestPath"] = "/api/v1/bookings", ["UserId"] = "u-1" }))
        {
            logger.LogWarning(new InvalidOperationException("boom"),
                "Booking {BookingId} for {PhoneNumber} failed in {ElapsedMs} ms with {StatusCode} on {RouteTemplate}",
                "b-9", "09121234567", 12.5, 409, "api/v1/bookings");
        }

        var row = LogEventRow.From(harness.Sink.Single());

        row.Level.Should().Be((short)LogEventLevel.Warning);
        row.Message.Should().Contain("0912*****67").And.NotContain("09121234567");
        row.MessageTemplate.Should().StartWith("Booking {BookingId}");
        row.Exception.Should().Contain("InvalidOperationException: boom");
        row.SourceContext.Should().Be("AsanRezerve.ServiceCatalog.Test");
        row.TraceId.Should().Be(activity.TraceId.ToHexString());
        row.SpanId.Should().NotBeNullOrEmpty();
        row.RequestPath.Should().Be("/api/v1/bookings");
        row.RouteTemplate.Should().Be("api/v1/bookings");
        row.StatusCode.Should().Be(409);
        row.ElapsedMs.Should().Be(12.5);
        row.UserId.Should().Be("u-1");
        row.Timestamp.Offset.Should().Be(TimeSpan.Zero);

        using var json = JsonDocument.Parse(row.Properties!);
        json.RootElement.GetProperty("BookingId").GetString().Should().Be("b-9");
        json.RootElement.TryGetProperty("SourceContext", out _).Should().BeFalse("promoted properties are columns, not JSON");
        json.RootElement.TryGetProperty("RequestPath", out _).Should().BeFalse();
    }

    [Fact]
    public void Phone_numbers_and_emails_in_exception_text_are_scrubbed()
    {
        var logEvent = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error,
            new InvalidOperationException("No user for 09121234567 / sara@example.com"),
            new MessageTemplateParser().Parse("Lookup failed for customer 09351112233"), []);

        var row = LogEventRow.From(logEvent);

        row.Exception.Should().Contain("0912*****67").And.Contain("s***@example.com").And.NotContain("09121234567");
        row.Message.Should().Be("Lookup failed for customer 0935*****33");
    }

    [Fact]
    public void Values_longer_than_their_column_are_cut_to_fit()
    {
        var row = LogEventRow.From(Event("x", LogEventLevel.Information,
            ("SourceContext", new string('s', 400)), ("RequestPath", new string('p', 900)), ("TraceId", "not-a-trace")));

        row.SourceContext!.Length.Should().Be(300);
        row.RequestPath!.Length.Should().Be(500);
    }

    [Fact]
    public async Task Nothing_is_written_before_the_store_exists_and_everything_after()
    {
        var batches = new RecordingBatchWriter();
        var writer = Writer(batches);
        await writer.StartAsync(CancellationToken.None);

        writer.TryEnqueue(Event("a")).Should().BeTrue();
        writer.TryEnqueue(Event("b")).Should().BeTrue();
        await writer.FlushAsync(TimeSpan.FromMilliseconds(200));
        batches.Rows.Should().BeEmpty("the migration has not created the table yet");

        writer.MarkReady();
        await writer.FlushAsync(TimeSpan.FromSeconds(10));

        batches.Rows.Select(r => r.Message).Should().Equal("a", "b");
        writer.Stats.Written.Should().Be(2);
        await writer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void A_full_queue_drops_and_counts_instead_of_blocking()
    {
        var writer = Writer(new RecordingBatchWriter(), capacity: 2);

        var accepted = Enumerable.Range(0, 5).Count(i => writer.TryEnqueue(Event($"e{i}")));

        accepted.Should().Be(2);
        writer.Stats.Dropped.Should().Be(3);
        writer.Stats.QueueLength.Should().Be(2);
    }

    [Fact]
    public async Task A_failing_database_is_retried_once_then_the_batch_is_dropped_and_counted()
    {
        var batches = new RecordingBatchWriter { FailuresLeft = 2 };
        var writer = Writer(batches);
        writer.MarkReady();
        await writer.StartAsync(CancellationToken.None);

        writer.TryEnqueue(Event("lost"));
        await writer.FlushAsync(TimeSpan.FromSeconds(10));
        writer.TryEnqueue(Event("kept"));
        await writer.FlushAsync(TimeSpan.FromSeconds(10));

        batches.Rows.Select(r => r.Message).Should().Equal("kept");
        writer.Stats.FailedBatches.Should().Be(1);
        writer.Stats.LastError.Should().Contain("database down");
        await writer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Events_are_written_in_batches()
    {
        var batches = new RecordingBatchWriter();
        var writer = Writer(batches, batchSize: 2);
        for (var i = 0; i < 5; i++) writer.TryEnqueue(Event($"e{i}"));
        writer.MarkReady();
        await writer.StartAsync(CancellationToken.None);

        await writer.FlushAsync(TimeSpan.FromSeconds(10));

        batches.Batches.Should().OnlyContain(b => b.Count <= 2);
        batches.Rows.Should().HaveCount(5);
        await writer.StopAsync(CancellationToken.None);
    }
}

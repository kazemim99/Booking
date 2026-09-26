using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog.Events;

namespace AsanRezerve.Infrastructure.Observability.LogStore;

/// <summary>Health of the log store, for the admin overview.</summary>
public sealed record LogStoreStats(
    bool Ready,
    long Enqueued,
    long Written,
    long Dropped,
    long FailedBatches,
    int QueueLength,
    string? LastError,
    DateTimeOffset? LastWriteAt);

/// <summary>
/// Moves log events from the logging call to the database without ever making the caller wait (design D6).
/// <para><see cref="TryEnqueue"/> is a non-blocking write to a bounded channel; when it is full the event is
/// dropped and counted. A background loop drains batches (up to <see cref="LogStoreOptions.BatchSize"/>, or
/// whatever arrived within <see cref="LogStoreOptions.FlushIntervalMs"/>), maps them to rows and copies them in.
/// A failed batch is retried once, then dropped and counted. Nothing is written until <see cref="MarkReady"/>,
/// i.e. until the migration has created the table; startup events wait in the queue.</para>
/// <para>It never logs through <c>ILogger</c>: a failure to write logs must not produce more logs to write.</para>
/// </summary>
public sealed class LogStoreWriter : BackgroundService
{
    private readonly ILogEventBatchWriter _writer;
    private readonly LogStoreOptions _options;
    private readonly TimeProvider _time;
    private readonly Channel<LogEvent> _queue;
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private long _enqueued;
    private long _written;
    private long _dropped;
    private long _failedBatches;
    private long _inFlight;
    private int _flushRequests;
    private string? _lastError;
    private DateTimeOffset? _lastWriteAt;

    public LogStoreWriter(ILogEventBatchWriter writer, IOptions<LogStoreOptions> options, TimeProvider time)
    {
        _writer = writer;
        _options = options.Value;
        _time = time;
        _queue = Channel.CreateBounded<LogEvent>(new BoundedChannelOptions(Math.Max(1, _options.QueueCapacity))
        {
            // Wait, not DropWrite: DropWrite makes TryWrite report success while discarding, so drops could not be
            // counted. TryWrite never waits in either mode; it returns false when the queue is full.
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public LogStoreStats Stats => new(
        _ready.Task.IsCompleted,
        Interlocked.Read(ref _enqueued),
        Interlocked.Read(ref _written),
        Interlocked.Read(ref _dropped),
        Interlocked.Read(ref _failedBatches),
        _queue.Reader.Count,
        Volatile.Read(ref _lastError),
        _lastWriteAt);

    /// <summary>Queues an event; false (and counted) when the queue is full. Never blocks.</summary>
    public bool TryEnqueue(LogEvent logEvent)
    {
        // Count first: FlushAsync must never see zero while an accepted event is on its way into the queue.
        Interlocked.Increment(ref _inFlight);
        if (_queue.Writer.TryWrite(logEvent))
        {
            Interlocked.Increment(ref _enqueued);
            return true;
        }

        Interlocked.Decrement(ref _inFlight);
        Interlocked.Increment(ref _dropped);
        return false;
    }

    /// <summary>The table exists: start writing.</summary>
    public void MarkReady() => _ready.TrySetResult();

    /// <summary>Waits until every accepted event has been written or given up on, or the timeout passes.</summary>
    public async Task FlushAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var deadline = _time.GetUtcNow() + timeout;
        Interlocked.Increment(ref _flushRequests);
        try
        {
            while (Interlocked.Read(ref _inFlight) > 0 && _time.GetUtcNow() < deadline)
                await Task.Delay(TimeSpan.FromMilliseconds(10), _time, cancellationToken);
        }
        finally
        {
            Interlocked.Decrement(ref _flushRequests);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _ready.Task.WaitAsync(stoppingToken);

            var batch = new List<LogEvent>(_options.BatchSize);
            while (await _queue.Reader.WaitToReadAsync(stoppingToken))
            {
                await CollectAsync(batch, stoppingToken);
                await WriteAsync(batch, stoppingToken);
                batch.Clear();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Give queued events a last chance to be written on a graceful shutdown.
        if (_ready.Task.IsCompleted)
            await FlushAsync(TimeSpan.FromSeconds(5), cancellationToken);
        _queue.Writer.TryComplete();
        await base.StopAsync(cancellationToken);
    }

    private async Task CollectAsync(List<LogEvent> batch, CancellationToken stoppingToken)
    {
        var size = Math.Max(1, _options.BatchSize);
        var deadline = _time.GetUtcNow() + TimeSpan.FromMilliseconds(Math.Max(0, _options.FlushIntervalMs));

        while (batch.Count < size)
        {
            while (batch.Count < size && _queue.Reader.TryRead(out var next))
                batch.Add(next);

            if (batch.Count >= size || Volatile.Read(ref _flushRequests) > 0) return;

            var remaining = deadline - _time.GetUtcNow();
            if (remaining <= TimeSpan.Zero) return;

            using var wait = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            wait.CancelAfter(remaining);
            try
            {
                if (!await _queue.Reader.WaitToReadAsync(wait.Token)) return;
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task WriteAsync(List<LogEvent> batch, CancellationToken stoppingToken)
    {
        if (batch.Count == 0) return;

        try
        {
            var rows = batch.Select(LogEventRow.From).ToList();
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await _writer.WriteAsync(rows, stoppingToken);
                    Interlocked.Add(ref _written, rows.Count);
                    _lastWriteAt = _time.GetUtcNow();
                    return;
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    Volatile.Write(ref _lastError, $"{_time.GetUtcNow():O} {ex.GetType().Name}: {ex.Message}");
                    if (attempt >= 2)
                    {
                        Interlocked.Increment(ref _failedBatches);
                        return;
                    }
                }
            }
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // Mapping failed (a property that would not serialise): drop the batch, never the writer.
            Volatile.Write(ref _lastError, $"{_time.GetUtcNow():O} {ex.GetType().Name}: {ex.Message}");
            Interlocked.Increment(ref _failedBatches);
        }
        finally
        {
            Interlocked.Add(ref _inFlight, -batch.Count);
        }
    }
}

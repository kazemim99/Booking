using Npgsql;
using NpgsqlTypes;

namespace AsanRezerve.Infrastructure.Observability.LogStore;

/// <summary>
/// Binary <c>COPY</c> into <c>observability.log_events</c>: one round trip per batch. Uses its own data source,
/// built without a logger factory — logging the log writer's own SQL would feed it back into itself.
/// </summary>
public sealed class NpgsqlLogEventBatchWriter(LogStoreDataSource dataSource) : ILogEventBatchWriter
{
    private const string Copy =
        "COPY observability.log_events (timestamp, level, message, message_template, exception, source_context, " +
        "trace_id, span_id, request_path, route_template, status_code, elapsed_ms, user_id, properties) " +
        "FROM STDIN (FORMAT BINARY)";

    public async Task WriteAsync(IReadOnlyList<LogEventRow> rows, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.Value.OpenConnectionAsync(cancellationToken);
        await using var importer = await connection.BeginBinaryImportAsync(Copy, cancellationToken);

        foreach (var row in rows)
        {
            await importer.StartRowAsync(cancellationToken);
            await importer.WriteAsync(row.Timestamp, NpgsqlDbType.TimestampTz, cancellationToken);
            await importer.WriteAsync(row.Level, NpgsqlDbType.Smallint, cancellationToken);
            await importer.WriteAsync(row.Message, NpgsqlDbType.Text, cancellationToken);
            await WriteOrNull(importer, row.MessageTemplate, NpgsqlDbType.Text, cancellationToken);
            await WriteOrNull(importer, row.Exception, NpgsqlDbType.Text, cancellationToken);
            await WriteOrNull(importer, row.SourceContext, NpgsqlDbType.Varchar, cancellationToken);
            await WriteOrNull(importer, row.TraceId, NpgsqlDbType.Varchar, cancellationToken);
            await WriteOrNull(importer, row.SpanId, NpgsqlDbType.Varchar, cancellationToken);
            await WriteOrNull(importer, row.RequestPath, NpgsqlDbType.Varchar, cancellationToken);
            await WriteOrNull(importer, row.RouteTemplate, NpgsqlDbType.Varchar, cancellationToken);
            if (row.StatusCode is { } status) await importer.WriteAsync(status, NpgsqlDbType.Integer, cancellationToken);
            else await importer.WriteNullAsync(cancellationToken);
            if (row.ElapsedMs is { } elapsed) await importer.WriteAsync(elapsed, NpgsqlDbType.Double, cancellationToken);
            else await importer.WriteNullAsync(cancellationToken);
            await WriteOrNull(importer, row.UserId, NpgsqlDbType.Varchar, cancellationToken);
            await WriteOrNull(importer, row.Properties, NpgsqlDbType.Jsonb, cancellationToken);
        }

        await importer.CompleteAsync(cancellationToken);
    }

    private static Task WriteOrNull(NpgsqlBinaryImporter importer, string? value, NpgsqlDbType type, CancellationToken cancellationToken) =>
        value is null ? importer.WriteNullAsync(cancellationToken) : importer.WriteAsync(value, type, cancellationToken);
}

/// <summary>The log store's own connection pool, over <c>ConnectionStrings:DefaultConnection</c>.</summary>
public sealed class LogStoreDataSource(string connectionString) : IAsyncDisposable
{
    private readonly Lazy<NpgsqlDataSource> _dataSource = new(() => new NpgsqlDataSourceBuilder(connectionString).Build());

    public NpgsqlDataSource Value => _dataSource.Value;

    public async ValueTask DisposeAsync()
    {
        if (_dataSource.IsValueCreated) await _dataSource.Value.DisposeAsync();
    }
}

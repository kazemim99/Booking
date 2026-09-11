using Npgsql;

namespace Booksy.Tests.Common.Fixtures;

/// <summary>
/// Resets a test database to the state it had right after migrations, between every test.
///
/// <para>There is no reference data to preserve: no entity configuration or migration in this
/// repository calls <c>HasData</c>/<c>InsertData</c>/raw <c>INSERT</c>, and with
/// <c>Database:SeedOnStartup=false</c> the host writes no rows at startup either (measured
/// 2026-09-11 for docs/TEST_ARCHITECTURE_AUDIT.md Phase 2). The only tables that must never be
/// truncated are the two EF migrations-history tables — truncating them would make the host
/// re-run every migration against a schema that already has it.</para>
///
/// <para>Respawn was the obvious library choice and was deliberately not used: version 6.0.0 (the
/// only one targeting a current TFM) drags in <c>Microsoft.Data.SqlClient</c> for a suite that only
/// ever talks to PostgreSQL, and with nothing to preserve a single <c>TRUNCATE</c> is simpler than
/// configuring it. CAP's own <c>cap</c> schema is left alone: its rows are an outbox, accumulating
/// them across a run costs nothing, and truncating tables a background dispatcher may be reading is
/// the riskier choice for no benefit.</para>
/// </summary>
public sealed class DatabaseReset
{
    private readonly string _connectionString;
    private string[]? _tables;

    public DatabaseReset(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>Deletes every row from every table this reset knows about (all but the history tables).</summary>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var tables = await GetTablesAsync(cancellationToken);
        if (tables.Length == 0)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        // One statement: Postgres truncates the whole listed set together, so cross-table FK
        // ordering within a schema does not matter. RESTART IDENTITY covers the two identity
        // columns in the model (BreakPeriods.Id, staff_working_days.id); CASCADE is a no-op today
        // (no FK references a truncated table from outside this list) and is cheap insurance.
        command.CommandText = $"TRUNCATE TABLE {string.Join(", ", tables)} RESTART IDENTITY CASCADE";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Fixture self-test support: the tables (of the ones this reset manages) that currently hold
    /// at least one row. Used to prove a test starts from an empty database rather than assuming it.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetNonEmptyTablesAsync(CancellationToken cancellationToken = default)
    {
        var tables = await GetTablesAsync(cancellationToken);
        var nonEmpty = new List<string>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        foreach (var table in tables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT EXISTS(SELECT 1 FROM {table})";
            if (await command.ExecuteScalarAsync(cancellationToken) is true)
            {
                nonEmpty.Add(table);
            }
        }

        return nonEmpty;
    }

    /// <summary>
    /// Every base table in the schemas this database uses, minus the migrations-history tables,
    /// as quoted <c>"schema"."table"</c> identifiers. Discovered once per fixture and cached: the
    /// schema shape does not change between tests, and this factory may back a host that migrated
    /// only one of the two bounded contexts (the UserManagement suite, until
    /// docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 3 retargets it), so the set is whatever is
    /// actually present rather than a hard-coded pair of schema names.
    /// </summary>
    private async Task<string[]> GetTablesAsync(CancellationToken cancellationToken)
    {
        if (_tables is not null)
        {
            return _tables;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT table_schema, table_name
            FROM information_schema.tables
            WHERE table_schema IN ('ServiceCatalog', 'user_management')
              AND table_type = 'BASE TABLE'
              AND table_name <> '__EFMigrationsHistory'
            """;

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add($"\"{reader.GetString(0)}\".\"{reader.GetString(1)}\"");
        }

        _tables = tables.ToArray();
        return _tables;
    }
}

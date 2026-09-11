// ========================================
// Booksy.Tests.Commons/PostgresTestContainerFixture.cs
// ========================================
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Booksy.Tests.Common.Fixtures;

/// <summary>
/// Provides an isolated PostgreSQL database for an integration-test factory, backed by a single
/// PostgreSQL container shared across the whole test process.
///
/// <para><b>Why the container is shared.</b> <c>TestWebApplicationFactory</c> constructs one of these per
/// factory, and <c>IntegrationTestBase</c> declares <c>IClassFixture&lt;TFactory&gt;</c> — so xUnit builds
/// one factory <b>per test class</b> and runs test classes in parallel. When this type started a container
/// of its own, a full run of the ServiceCatalog suite brought up <b>102 concurrent
/// <c>postgres:16-alpine</c> containers</b>, which exhausts memory on a developer machine and could never
/// run on a CI runner. The suite was effectively unrunnable, which is a large part of why it had been
/// disabled in CI.</para>
///
/// <para><b>Why a database per fixture rather than a shared one.</b> Sharing the server but not the
/// database keeps every test class on its own schema and data, so parallel classes cannot see each other's
/// rows — the isolation <c>IClassFixture</c> implies is preserved. Creating a database is milliseconds;
/// starting a container is seconds plus hundreds of megabytes.</para>
///
/// <para>Note the file already contained a <see cref="PostgresTestCollection"/> collection fixture intended
/// to share the container, but nothing ever used it: the factory bypassed it by newing this type up
/// directly. Making the container static achieves the same goal without requiring all ~40 test classes to
/// opt into a collection.</para>
/// </summary>
public sealed class PostgresTestContainerFixture : IAsyncLifetime
{
    // This type used to set `Npgsql.EnableLegacyTimestampBehavior` in a static constructor, mirroring
    // the three production hosts, on the stated grounds that the columns were
    // `timestamp without time zone`. They never were: all 162 timestamp columns across both contexts
    // are `timestamp with time zone`, so the switch bought nothing and cost correctness — under it
    // Npgsql returns `Kind=Local` on read, and every comparison against `DateTime.UtcNow` came out
    // wrong by the machine's UTC offset. Removed with FOLLOW-UPS #48; see
    // openspec/changes/_inline/utc-instants-end-to-end.

    // One server for the whole test process. Guarded because xUnit starts test classes in parallel, so
    // several fixtures can race to initialise it.
    private static readonly SemaphoreSlim ServerGate = new(1, 1);
    private static PostgreSqlContainer? _sharedServer;
    private static string? _serverConnectionString;

    private string? _databaseName;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var serverConnectionString = await EnsureSharedServerAsync();

        // A database per fixture. The name must be unique across the process and a valid identifier.
        _databaseName = $"booksy_test_{Guid.NewGuid():N}";

        await using (var connection = new NpgsqlConnection(serverConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            // The name is a server-generated GUID, not user input, and quoting keeps it a single
            // identifier regardless.
            command.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
            await command.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(serverConnectionString)
        {
            Database = _databaseName
        }.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (_databaseName is null || _serverConnectionString is null)
        {
            return;
        }

        try
        {
            await using var connection = new NpgsqlConnection(_serverConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            // WITH (FORCE) drops the database even if a pooled connection is still open; without it a
            // lingering connection from the factory's provider would make the drop fail and leak the
            // database for the rest of the run.
            command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            // A failed cleanup must never fail the test run: the container is torn down with the process
            // (Testcontainers' Ryuk reaper), so a leaked database costs nothing beyond this run.
            Console.WriteLine($"⚠️  Could not drop test database {_databaseName}: {ex.Message}");
        }

        // The shared server is deliberately NOT stopped here — other fixtures are still using it, and
        // Ryuk removes it when the test process exits.
        _databaseName = null;
    }

    private static async Task<string> EnsureSharedServerAsync()
    {
        if (_serverConnectionString is not null)
        {
            return _serverConnectionString;
        }

        await ServerGate.WaitAsync();
        try
        {
            if (_serverConnectionString is not null)
            {
                return _serverConnectionString;
            }

            var container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("booksy_test")
                .WithUsername("test_user")
                .WithPassword("test_password")
                .WithPortBinding(5432, true) // Random host port
                .WithCleanUp(true)
                .Build();

            await container.StartAsync();

            _sharedServer = container;
            _serverConnectionString = container.GetConnectionString();

            Console.WriteLine("✅ Shared PostgreSQL Testcontainer started (one per test process)");
            Console.WriteLine($"📦 Connection: {MaskPassword(_serverConnectionString)}");

            return _serverConnectionString;
        }
        finally
        {
            ServerGate.Release();
        }
    }

    private static string MaskPassword(string connectionString)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"Password=([^;]+)",
            "Password=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}

/// <summary>
/// xUnit collection fixture that shares a <see cref="PostgresTestContainerFixture"/> across test classes.
/// </summary>
/// <remarks>
/// Retained for tests that want to share one <i>database</i> as well as one server. It is not what keeps
/// the container count down — the container is process-wide static — so a class that does not join this
/// collection still gets an isolated database on the same server.
/// </remarks>
[CollectionDefinition(nameof(PostgresTestCollection))]
public class PostgresTestCollection : ICollectionFixture<PostgresTestContainerFixture>
{
    // No code. It exists solely to carry [CollectionDefinition] and the ICollectionFixture<> interface.
}

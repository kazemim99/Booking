// ========================================
// Booksy.Tests.Common/Fixtures/PostgresTestContainerFixture.cs
// ========================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Booksy.Tests.Common.Fixtures;

/// <summary>
/// PostgreSQL Testcontainer fixture for integration tests
/// Manages a real PostgreSQL instance in Docker for testing
/// </summary>
public sealed class PostgresTestContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;

    public string ConnectionString { get; private set; } = string.Empty;

    public PostgresTestContainerFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("booksy_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithPortBinding(5432, true) // Random port
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        ConnectionString = _postgresContainer.GetConnectionString();

        Console.WriteLine($"✅ PostgreSQL Testcontainer started");
        Console.WriteLine($"📦 Connection: {MaskPassword(ConnectionString)}");
    }

    public async Task DisposeAsync()
    {

        Console.WriteLine($"🛑 PostgreSQL Testcontainer stopped");
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
/// xUnit Collection Fixture to share PostgreSQL container across test classes
/// </summary>
[CollectionDefinition(nameof(PostgresTestCollection))]
public class PostgresTestCollection : ICollectionFixture<PostgresTestContainerFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
using System.Data.Common;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.Tests.Common.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NSubstitute;

namespace Booksy.Host.IntegrationTests.ServiceCatalog.Persistence;

/// <summary>
/// The review-moderation migration against rows that already exist — which is the only case that matters in
/// production, and the one the shared host can never exercise.
/// </summary>
/// <remarks>
/// <para><b>Why this does not use the shared host.</b> <c>BooksyHostFactory</c> migrates a fresh empty database
/// at startup, so by the time a test body runs the backfill has already been applied to zero rows and proves
/// nothing. This class takes its own database, stops the schema at the last migration before moderation,
/// writes rows the way the old code wrote them, and only then migrates forward.</para>
///
/// <para>This is the sole coverage for the "WHEN the migration runs" scenarios in the review-moderation and
/// review-engagement specs.</para>
/// </remarks>
public sealed class ReviewModerationMigrationTests
{
    /// <summary>The head of the ServiceCatalog migrations before this change.</summary>
    private const string LastMigrationBeforeModeration = "20260920164731_AddDeviceTokens";

    // Persisted by name, like the module's other enums.
    private const string Pending = "Pending";
    private const string Published = "Published";

    [Fact]
    public async Task Existing_reviews_and_replies_are_published_and_their_counters_survive_as_the_baseline()
    {
        // A database of its own: each test here needs a known starting schema, independent of test order.
        await using var database = await OwnDatabaseAsync();
        await using var context = NewContext(database);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(LastMigrationBeforeModeration);

        var withReply = Guid.NewGuid();
        var withoutReply = Guid.NewGuid();
        await InsertLegacyReviewAsync(context, withReply, helpful: 7, notHelpful: 2, reply: "ممنون از حضورتان");
        await InsertLegacyReviewAsync(context, withoutReply, helpful: 0, notHelpful: 0, reply: null);

        await migrator.MigrateAsync();

        var replied = await ReadReviewAsync(context, withReply);
        Assert.Equal(Published, replied.ModerationStatus);
        Assert.Equal(Published, replied.ReplyModerationStatus);
        Assert.Equal(7, replied.LegacyHelpful);
        Assert.Equal(2, replied.LegacyNotHelpful);
        Assert.Equal(0, replied.HelpfulVotes);
        Assert.Equal(0, replied.NotHelpfulVotes);
        Assert.NotNull(replied.FirstPublishedAt);

        var unreplied = await ReadReviewAsync(context, withoutReply);
        Assert.Equal(Published, unreplied.ModerationStatus);
        Assert.Null(unreplied.ReplyModerationStatus);

        // The legacy counts have no users behind them, so no vote is invented to back them.
        Assert.Equal(0L, await ScalarAsync<long>(context, "SELECT COUNT(*) FROM \"ServiceCatalog\".\"ReviewVotes\""));
    }

    [Fact]
    public async Task A_row_written_by_pre_moderation_code_after_the_migration_lands_in_the_queue()
    {
        // During a rolling deploy the previous image still INSERTs with its own column list. Every new NOT NULL
        // column needs a database default, or that INSERT fails 23502 — and the default must be Pending, so the
        // row waits for a moderator instead of going straight to the public.
        await using var database = await OwnDatabaseAsync();
        await using var context = NewContext(database);
        await context.GetService<IMigrator>().MigrateAsync();

        var id = Guid.NewGuid();
        await InsertLegacyReviewAsync(context, id, helpful: 0, notHelpful: 0, reply: null);

        var row = await ReadReviewAsync(context, id);
        Assert.Equal(Pending, row.ModerationStatus);
        Assert.Equal(0, row.HelpfulVotes);
        Assert.Equal(0, row.NotHelpfulVotes);
    }

    private static async Task<OwnedDatabase> OwnDatabaseAsync()
    {
        var fixture = new PostgresTestContainerFixture();
        await fixture.InitializeAsync();
        return new OwnedDatabase(fixture);
    }

    private sealed class OwnedDatabase(PostgresTestContainerFixture fixture) : IAsyncDisposable
    {
        public string ConnectionString => fixture.ConnectionString;
        public async ValueTask DisposeAsync() => await fixture.DisposeAsync();
    }

    private static ServiceCatalogDbContext NewContext(OwnedDatabase database)
    {
        var options = new DbContextOptionsBuilder<ServiceCatalogDbContext>()
            .UseNpgsql(database.ConnectionString, b =>
            {
                b.MigrationsAssembly("Booksy.ServiceCatalog.Infrastructure");
                b.MigrationsHistoryTable("__EFMigrationsHistory", "ServiceCatalog");
            })
            .Options;

        return new ServiceCatalogDbContext(
            options,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IDateTimeProvider>());
    }

    /// <summary>Exactly the columns the pre-moderation mapping wrote, including the soft-delete flag.</summary>
    private static Task InsertLegacyReviewAsync(
        ServiceCatalogDbContext context, Guid id, int helpful, int notHelpful, string? reply) =>
        context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ServiceCatalog"."Reviews"
                ("ReviewId", "ProviderId", "CustomerId", "BookingId", "RatingValue", "Comment", "IsVerified",
                 "ProviderResponse", "ProviderResponseAt", "HelpfulCount", "NotHelpfulCount", "CreatedAt",
                 "CreatedBy", "Version", "IsDeleted")
            VALUES
                ({id}, {Guid.NewGuid()}, {Guid.NewGuid()}, {Guid.NewGuid()}, {4.5m}, {"کار تمیز و دقیقی بود"}, {true},
                 {reply}, {(reply is null ? (DateTime?)null : DateTime.UtcNow)}, {helpful}, {notHelpful},
                 {DateTime.UtcNow}, {"legacy"}, {0}, {false})
            """);

    private sealed record ReviewRow(
        string ModerationStatus,
        string? ReplyModerationStatus,
        int LegacyHelpful,
        int LegacyNotHelpful,
        int HelpfulVotes,
        int NotHelpfulVotes,
        DateTime? FirstPublishedAt);

    private static async Task<ReviewRow> ReadReviewAsync(ServiceCatalogDbContext context, Guid id)
    {
        await using var command = await OpenCommandAsync(context, """
            SELECT "ModerationStatus", "ReplyModerationStatus", "HelpfulCount", "NotHelpfulCount",
                   "HelpfulVoteCount", "NotHelpfulVoteCount", "FirstPublishedAt"
            FROM "ServiceCatalog"."Reviews" WHERE "ReviewId" = @id
            """);
        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = id;
        command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync(), $"review {id} not found");
        return new ReviewRow(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.IsDBNull(6) ? null : reader.GetDateTime(6));
    }

    private static async Task<T> ScalarAsync<T>(ServiceCatalogDbContext context, string sql)
    {
        await using var command = await OpenCommandAsync(context, sql);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<DbCommand> OpenCommandAsync(ServiceCatalogDbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        return command;
    }
}

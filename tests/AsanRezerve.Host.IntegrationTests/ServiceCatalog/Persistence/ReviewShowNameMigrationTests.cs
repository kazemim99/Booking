using AsanRezerve.Core.Application.Abstractions.Services;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using AsanRezerve.Tests.Common.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NSubstitute;

namespace AsanRezerve.Host.IntegrationTests.ServiceCatalog.Persistence;

/// <summary>
/// The reviews-and-reschedule-round2 migration against rows that already exist: every review written before the
/// author could hide their name keeps showing it (D2). Own database, stopped before the migration — the shared host
/// migrates an empty one at startup, which proves nothing (see <see cref="ReviewModerationMigrationTests"/>).
/// </summary>
public sealed class ReviewShowNameMigrationTests
{
    /// <summary>The head of the ServiceCatalog migrations before this change.</summary>
    private const string LastMigrationBefore = "20260921213139_AddReviewModerationAndVoting";

    [Fact]
    public async Task Existing_reviews_keep_showing_their_authors_name()
    {
        var fixture = new PostgresTestContainerFixture();
        await fixture.InitializeAsync();
        try
        {
            await using var context = NewContext(fixture.ConnectionString);
            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(LastMigrationBefore);

            var id = Guid.NewGuid();
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "ServiceCatalog"."Reviews"
                    ("ReviewId", "ProviderId", "CustomerId", "BookingId", "RatingValue", "Comment", "IsVerified",
                     "HelpfulCount", "NotHelpfulCount", "CreatedAt", "CreatedBy", "Version", "IsDeleted")
                VALUES
                    ({id}, {Guid.NewGuid()}, {Guid.NewGuid()}, {Guid.NewGuid()}, {4.5m}, {"کار تمیز و دقیقی بود"}, {true},
                     {0}, {0}, {DateTime.UtcNow}, {"before-show-name"}, {0}, {false})
                """);

            await migrator.MigrateAsync();

            var showName = await context.Database
                .SqlQuery<bool>($"""SELECT "ShowName" AS "Value" FROM "ServiceCatalog"."Reviews" WHERE "ReviewId" = {id}""")
                .SingleAsync();
            Assert.True(showName);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private static ServiceCatalogDbContext NewContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ServiceCatalogDbContext>()
            .UseNpgsql(connectionString, b =>
            {
                b.MigrationsAssembly("AsanRezerve.ServiceCatalog.Infrastructure");
                b.MigrationsHistoryTable("__EFMigrationsHistory", "ServiceCatalog");
            })
            .Options;

        return new ServiceCatalogDbContext(
            options,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IDateTimeProvider>());
    }
}

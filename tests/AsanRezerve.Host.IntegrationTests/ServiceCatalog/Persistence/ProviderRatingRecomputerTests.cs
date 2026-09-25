using AsanRezerve.Core.Application.Abstractions.Services;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using AsanRezerve.ServiceCatalog.Infrastructure.Reviews;
using AsanRezerve.Tests.Common.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NSubstitute;

namespace AsanRezerve.Host.IntegrationTests.ServiceCatalog.Persistence;

/// <summary>
/// The rating recompute against real Postgres — above all, that it sees changes the command has made but the
/// unit of work has not yet flushed.
/// </summary>
/// <remarks>
/// Commands run inside one transaction and the unit of work saves only after the handler returns. A recompute
/// that only queried the database would therefore read the review as it was before the approve/hide that
/// triggered it, and every provider's rating would sit one moderation action behind. That is the defect design
/// D6 exists to prevent, so it is tested directly here; the end-to-end path through the moderation endpoints is
/// task 4.3.
/// </remarks>
public sealed class ProviderRatingRecomputerTests : IClassFixture<PostgresTestContainerFixture>
{
    private const string Admin = "Admin:moderator";
    private readonly PostgresTestContainerFixture _database;
    private bool _migrated;

    public ProviderRatingRecomputerTests(PostgresTestContainerFixture database) => _database = database;

    [Fact]
    public async Task An_approval_the_unit_of_work_has_not_saved_yet_is_already_counted()
    {
        var providerId = await SeedProviderAsync();
        var pending = await SeedReviewAsync(providerId, 2.0m, publish: false);
        await SeedReviewAsync(providerId, 4.0m, publish: true);

        await using (var context = await NewContextAsync())
        {
            var review = await context.Reviews.SingleAsync(r => r.Id == pending);
            review.Publish(Admin); // tracked, NOT saved

            await new ProviderRatingRecomputer(context).RecomputeAsync(providerId);
            await context.SaveChangesAsync();
        }

        var (average, count) = await ReadProviderRatingAsync(providerId);
        Assert.Equal(2, count);
        Assert.Equal(3.0m, average);
    }

    [Fact]
    public async Task A_hide_the_unit_of_work_has_not_saved_yet_is_already_excluded()
    {
        var providerId = await SeedProviderAsync();
        var hidden = await SeedReviewAsync(providerId, 1.0m, publish: true);
        await SeedReviewAsync(providerId, 5.0m, publish: true);

        await using (var context = await NewContextAsync())
        {
            var review = await context.Reviews.SingleAsync(r => r.Id == hidden);
            review.Hide("reported and upheld", Admin);

            await new ProviderRatingRecomputer(context).RecomputeAsync(providerId);
            await context.SaveChangesAsync();
        }

        var (average, count) = await ReadProviderRatingAsync(providerId);
        Assert.Equal(1, count);
        Assert.Equal(5.0m, average);
    }

    [Fact]
    public async Task A_provider_whose_last_published_review_goes_is_unrated_again()
    {
        var providerId = await SeedProviderAsync();
        var only = await SeedReviewAsync(providerId, 4.0m, publish: true);
        await RecomputeAndSaveAsync(providerId);

        await using (var context = await NewContextAsync())
        {
            (await context.Reviews.SingleAsync(r => r.Id == only)).Hide("spam", Admin);
            await new ProviderRatingRecomputer(context).RecomputeAsync(providerId);
            await context.SaveChangesAsync();
        }

        var (average, count) = await ReadProviderRatingAsync(providerId);
        Assert.Equal(0, count);
        Assert.Equal(0m, average);
    }

    [Fact]
    public async Task Other_providers_reviews_never_leak_in()
    {
        var mine = await SeedProviderAsync();
        var theirs = await SeedProviderAsync();
        await SeedReviewAsync(mine, 5.0m, publish: true);
        await SeedReviewAsync(theirs, 1.0m, publish: true);

        await RecomputeAndSaveAsync(mine);

        var (average, count) = await ReadProviderRatingAsync(mine);
        Assert.Equal(1, count);
        Assert.Equal(5.0m, average);
    }

    [Fact]
    public async Task Dimension_averages_are_written_to_the_summary_over_only_those_who_rated_them()
    {
        var providerId = await SeedProviderAsync();
        await SeedReviewAsync(providerId, 4.0m, publish: true, new ReviewDimensionRatings(Punctuality: 4.0m));
        await SeedReviewAsync(providerId, 5.0m, publish: true, new ReviewDimensionRatings(Punctuality: 5.0m, Skill: 5.0m));
        await SeedReviewAsync(providerId, 3.0m, publish: true);

        await RecomputeAndSaveAsync(providerId);
        // Recomputing again must overwrite the same row, not add a second.
        await RecomputeAndSaveAsync(providerId);

        await using var context = await NewContextAsync();
        var summary = await context.ProviderRatingSummaries.AsNoTracking().SingleAsync(s => s.ProviderId == providerId.Value);
        Assert.Equal(4.5m, summary.PunctualityAverage);
        Assert.Equal(2, summary.PunctualityCount);
        Assert.Equal(5.0m, summary.SkillAverage);
        Assert.Equal(1, summary.SkillCount);
        Assert.Null(summary.CleanlinessAverage);
        Assert.Equal(0, summary.CleanlinessCount);
    }

    // ── Arrange helpers ──

    private async Task<ServiceCatalogDbContext> NewContextAsync()
    {
        var options = new DbContextOptionsBuilder<ServiceCatalogDbContext>()
            .UseNpgsql(_database.ConnectionString, b =>
            {
                b.MigrationsAssembly("AsanRezerve.ServiceCatalog.Infrastructure");
                b.MigrationsHistoryTable("__EFMigrationsHistory", "ServiceCatalog");
            })
            .Options;

        var context = new ServiceCatalogDbContext(
            options, Substitute.For<ICurrentUserService>(), Substitute.For<IDateTimeProvider>());

        if (!_migrated)
        {
            await context.GetService<IMigrator>().MigrateAsync();
            _migrated = true;
        }

        return context;
    }

    private async Task<ProviderId> SeedProviderAsync()
    {
        await using var context = await NewContextAsync();
        var provider = Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()),
            "سالن آزمایشی",
            "Description",
            ServiceCategory.HairSalon,
            ContactInfo.Create(Email.Create($"owner-{Guid.NewGuid():N}@salon.example"), PhoneNumber.From("+989120000000")),
            BusinessAddress.Create("خیابان ولیعصر، پلاک ۱۲", "خیابان ولیعصر", "تهران", "تهران", "1234567890", "IR"));
        context.Providers.Add(provider);
        await context.SaveChangesAsync();
        return provider.Id;
    }

    private async Task<Guid> SeedReviewAsync(
        ProviderId providerId, decimal overall, bool publish, ReviewDimensionRatings? dimensions = null)
    {
        await using var context = await NewContextAsync();
        var review = Review.Create(providerId, UserId.From(Guid.NewGuid()), Guid.NewGuid(), overall, dimensions: dimensions);
        if (publish) review.Publish(Admin);
        context.Reviews.Add(review);
        await context.SaveChangesAsync();
        return review.Id;
    }

    private async Task RecomputeAndSaveAsync(ProviderId providerId)
    {
        await using var context = await NewContextAsync();
        await new ProviderRatingRecomputer(context).RecomputeAsync(providerId);
        await context.SaveChangesAsync();
    }

    private async Task<(decimal Average, int Count)> ReadProviderRatingAsync(ProviderId providerId)
    {
        await using var context = await NewContextAsync();
        var provider = await context.Providers.AsNoTracking().SingleAsync(p => p.Id == providerId);
        return (provider.AverageRating, provider.PublishedReviewCount);
    }
}

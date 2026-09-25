using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Seeders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.Infrastructure;

/// <summary>
/// openspec/changes/provider-onboarding-ux S1/S2. Production shipped with ZERO rows in
/// ProvinceCities: province/city reference data and fake demo data sat behind one
/// Database:SeedOnStartup gate that is off outside Development, so production got neither and the
/// provider onboarding city picker could never match a city. Reference data now has its own entry
/// point. These tests pin the two things that matter about it: it fills the geography, and it
/// never plants demo businesses in a real database.
/// <para>
/// Each test starts from an empty database (IntegrationTestBase resets every table first), and this
/// host runs with Database:SeedReferenceData=false, so anything present afterwards was put there by
/// the call under test.
/// </para>
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class ReferenceDataSeedingTests : ServiceCatalogIntegrationTestBase
{
    public ReferenceDataSeedingTests(AsanRezerveHostFactory factory)
        : base(factory)
    {
    }

    private async Task SeedReferenceDataAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ServiceCatalogDatabaseSeederOrchestrator>();
        await orchestrator.SeedReferenceDataAsync();
    }

    private async Task<T> QueryAsync<T>(Func<ServiceCatalogDbContext, Task<T>> query)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await query(db);
    }

    [Fact]
    public async Task Reference_seeding_fills_the_province_city_hierarchy_the_city_picker_reads()
    {
        await SeedReferenceDataAsync();

        var cities = await QueryAsync(db => db.ProvinceCities.CountAsync());
        cities.Should().BeGreaterThan(100,
            "the onboarding city picker searches this table; with it empty, typing a city name can " +
            "never match anything (what production showed before this fix)");

        // The user's own reported case: typing «پارس» must be able to find «پارس‌آباد».
        var parsabad = await QueryAsync(db => db.ProvinceCities
            .AnyAsync(c => c.Name.Contains("پارس")));
        parsabad.Should().BeTrue("a city matching «پارس» must be selectable");
    }

    [Fact]
    public async Task Reference_seeding_seeds_notification_templates()
    {
        await SeedReferenceDataAsync();

        var templates = await QueryAsync(db => db.NotificationTemplates.CountAsync());
        templates.Should().BeGreaterThan(0,
            "INotificationTemplateService renders booking/payment notifications from these at runtime");
    }

    [Fact]
    public async Task Reference_seeding_never_creates_demo_businesses()
    {
        await SeedReferenceDataAsync();

        var providers = await QueryAsync(db => db.Providers.CountAsync());
        var services = await QueryAsync(db => db.Services.CountAsync());

        providers.Should().Be(0,
            "reference seeding runs in PRODUCTION by default — it must never plant fake salons there");
        services.Should().Be(0, "nor fake services");
    }

    [Fact]
    public async Task Reference_seeding_is_idempotent_across_restarts()
    {
        await SeedReferenceDataAsync();
        var first = await QueryAsync(db => db.ProvinceCities.CountAsync());

        await SeedReferenceDataAsync();
        var second = await QueryAsync(db => db.ProvinceCities.CountAsync());

        second.Should().Be(first, "it runs on every production startup, so a restart must not duplicate rows");
    }
}

using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.Tests.Common.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Persistence;

/// <summary>
/// Persistence and query coverage for the provider category model
/// (<c>refactor-provider-category-model</c>).
///
/// <para>Three things this locks down, each of which was broken at some point in the refactor:</para>
/// <list type="number">
/// <item><description><c>PrimaryCategory</c> / <c>Service.Category</c> round-trip through the int
/// column mapping rather than silently landing on the wrong member.</description></item>
/// <item><description>The category repository lookups translate to SQL. They previously compared
/// <c>Category.ToString()</c> against a string, which has no translation for a
/// <c>HasConversion&lt;int&gt;()</c> column, so the queries threw at runtime.</description></item>
/// <item><description>The database refuses the unset category <c>0</c>, which is what
/// <c>20251223143438_RemoveStaff</c> backfilled into every pre-existing row and what
/// <c>20260815222542_BackfillProviderPrimaryCategory</c> remediates.</description></item>
/// </list>
/// </summary>
public class ProviderCategoryPersistenceTests : Infrastructure.ServiceCatalogIntegrationTestBase
{
    public ProviderCategoryPersistenceTests(
        Infrastructure.ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    private async Task<Provider> PersistProviderAsync(ServiceCategory category)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IProviderWriteRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();

        var provider = new ProviderBuilder()
            .WithCategory(category)
            .WithBusinessName($"Test {category} {Guid.NewGuid():N}")
            .AsActive()
            .Build();

        await repo.SaveProviderAsync(provider);
        await uow.CommitAsync();
        return provider;
    }

    // ---------------------------------------------------------------- round-tripping

    [Theory]
    [InlineData(ServiceCategory.HairSalon)]
    [InlineData(ServiceCategory.Barbershop)]
    [InlineData(ServiceCategory.MedicalClinic)]
    [InlineData(ServiceCategory.PetCare)]
    public async Task Provider_primary_category_round_trips_through_the_database(ServiceCategory category)
    {
        var provider = await PersistProviderAsync(category);

        var reloaded = await FindProviderAsync(provider.Id.Value);

        reloaded.Should().NotBeNull();
        reloaded!.PrimaryCategory.Should().Be(category);
    }

    [Fact]
    public async Task Provider_category_is_stored_as_its_enum_integer()
    {
        // The int is the cross-boundary contract (frontend enum, category slugs, search filters),
        // so confirm the column really holds the declared number and not an ordinal.
        var provider = await PersistProviderAsync(ServiceCategory.Dental);

        var stored = await DbContext.Database
            .SqlQuery<int>($@"SELECT ""PrimaryCategory"" AS ""Value"" FROM ""ServiceCatalog"".""Providers"" WHERE ""Id"" = {provider.Id.Value}")
            .SingleAsync();

        stored.Should().Be(10);
    }

    // ---------------------------------------------------------------- category lookups translate to SQL

    [Fact]
    public async Task Provider_lookup_by_category_returns_only_that_category()
    {
        var yoga = await PersistProviderAsync(ServiceCategory.Yoga);
        await PersistProviderAsync(ServiceCategory.Automotive);

        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IProviderReadRepository>();

        var results = await repo.GetByCategoryAsync(ServiceCategory.Yoga);

        results.Should().Contain(p => p.Id == yoga.Id);
        results.Should().OnlyContain(p => p.PrimaryCategory == ServiceCategory.Yoga);
    }

    [Fact]
    public async Task Service_lookup_by_category_returns_only_that_category()
    {
        var provider = await PersistProviderAsync(ServiceCategory.Spa);

        using (var scope = Factory.Services.CreateScope())
        {
            var serviceRepo = scope.ServiceProvider.GetRequiredService<IServiceWriteRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();

            await serviceRepo.SaveServiceAsync(Service.Create(
                provider.Id, "Hot Stone Massage", "Relaxing", ServiceCategory.Massage,
                ServiceType.Standard, Price.Create(800_000m, "IRR"), Duration.FromMinutes(60)));
            await serviceRepo.SaveServiceAsync(Service.Create(
                provider.Id, "Facial", "Skincare", ServiceCategory.BeautySalon,
                ServiceType.Standard, Price.Create(600_000m, "IRR"), Duration.FromMinutes(45)));

            await uow.CommitAsync();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IServiceReadRepository>();

            // Before the fix this threw: `s.Category.ToString() == category` has no SQL translation.
            var results = await repo.GetByCategoryAsync(ServiceCategory.Massage);

            results.Should().NotBeEmpty();
            results.Should().OnlyContain(s => s.Category == ServiceCategory.Massage);
            results.Should().Contain(s => s.Name == "Hot Stone Massage");
        }
    }

    [Fact]
    public async Task Average_price_by_category_translates_and_ignores_other_categories()
    {
        var provider = await PersistProviderAsync(ServiceCategory.Tutoring);

        using (var scope = Factory.Services.CreateScope())
        {
            var serviceRepo = scope.ServiceProvider.GetRequiredService<IServiceWriteRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();

            await serviceRepo.SaveServiceAsync(Service.Create(
                provider.Id, "Maths tutoring", "One to one", ServiceCategory.Tutoring,
                ServiceType.Standard, Price.Create(1_000_000m, "IRR"), Duration.FromMinutes(60)));
            await serviceRepo.SaveServiceAsync(Service.Create(
                provider.Id, "Physics tutoring", "One to one", ServiceCategory.Tutoring,
                ServiceType.Standard, Price.Create(2_000_000m, "IRR"), Duration.FromMinutes(60)));

            await uow.CommitAsync();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IServiceReadRepository>();

            var average = await repo.GetAveragePriceByCategoryAsync(ServiceCategory.Tutoring, "IRR");

            average.Should().Be(1_500_000m);
        }
    }

    // ---------------------------------------------------------------- the database refuses unset categories

    [Fact]
    public async Task Database_rejects_a_provider_whose_category_is_the_unset_zero()
    {
        // Guards the remediation migration: nothing may reintroduce the unrenderable 0 that
        // RemoveStaff's `DEFAULT 0` left on every pre-existing row.
        var provider = await PersistProviderAsync(ServiceCategory.Gym);

        var write = async () => await DbContext.Database.ExecuteSqlRawAsync(
            @"UPDATE ""ServiceCatalog"".""Providers"" SET ""PrimaryCategory"" = 0 WHERE ""Id"" = {0}",
            provider.Id.Value);

        (await write.Should().ThrowAsync<Npgsql.PostgresException>())
            .Which.ConstraintName.Should().Be("CK_Providers_PrimaryCategory_Assigned");
    }

    [Fact]
    public async Task Database_rejects_a_service_whose_category_is_the_unset_zero()
    {
        var provider = await PersistProviderAsync(ServiceCategory.Physiotherapy);
        Guid serviceId;

        using (var scope = Factory.Services.CreateScope())
        {
            var serviceRepo = scope.ServiceProvider.GetRequiredService<IServiceWriteRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();

            var service = Service.Create(
                provider.Id, "Rehab session", "Recovery", ServiceCategory.Physiotherapy,
                ServiceType.Standard, Price.Create(900_000m, "IRR"), Duration.FromMinutes(45));
            serviceId = service.Id.Value;

            await serviceRepo.SaveServiceAsync(service);
            await uow.CommitAsync();
        }

        var write = async () => await DbContext.Database.ExecuteSqlRawAsync(
            @"UPDATE ""ServiceCatalog"".""Services"" SET ""Category"" = 0 WHERE ""Id"" = {0}",
            serviceId);

        (await write.Should().ThrowAsync<Npgsql.PostgresException>())
            .Which.ConstraintName.Should().Be("CK_Services_Category_Assigned");
    }
}

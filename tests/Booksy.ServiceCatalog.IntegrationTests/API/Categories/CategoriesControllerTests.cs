using System.Net;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Queries.Category.GetCategoriesWithCounts;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Application.Queries.Provider.SearchProviders;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Enums.Extensions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.Tests.Common.Builders;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Categories;

/// <summary>
/// Integration tests for the public category endpoints
/// (<c>refactor-provider-category-model</c>, spec: Category-Based Provider Discovery).
///
/// <para>These cover the browse contract the frontend depends on: the full taxonomy is always
/// returned with a stable numeric id, empty categories are surfaced as "coming soon" rather than
/// hidden, "popular" is the bookable subset ranked by size, and a category page can be reached by
/// either id or slug.</para>
/// </summary>
public class CategoriesControllerTests : Infrastructure.ServiceCatalogIntegrationTestBase
{
    public CategoriesControllerTests(
        Infrastructure.ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    private async Task SeedActiveProviderAsync(ServiceCategory category)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IProviderWriteRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();

        var provider = new ProviderBuilder()
            .WithCategory(category)
            .WithBusinessName($"Cat {category} {Guid.NewGuid():N}")
            .AsActive()
            .Build();

        await repo.SaveProviderAsync(provider);
        await uow.CommitAsync();
    }

    #region GET /api/v1/categories

    [Fact]
    public async Task Browsing_categories_returns_the_whole_taxonomy()
    {
        // The taxonomy is the enum, not "whatever has providers" — a half-empty catalogue would
        // make the browse page look broken.
        var response = await GetAsync<List<CategoryWithCountViewModel>>("/api/v1/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Should().HaveCount(ServiceCategoryExtensions.GetAll().Length);
    }

    [Fact]
    public async Task Browsing_categories_is_anonymous()
    {
        // Category discovery must work before a customer signs in.
        ClearAuthentication();

        var raw = await GetAsync("/api/v1/categories");

        raw.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Every_browsed_category_carries_the_metadata_the_frontend_renders()
    {
        var response = await GetAsync<List<CategoryWithCountViewModel>>("/api/v1/categories");

        response.Data.Should().NotBeNull();
        response.Data!.Should().OnlyContain(c =>
            c.Id >= 1 &&
            !string.IsNullOrWhiteSpace(c.Key) &&
            !string.IsNullOrWhiteSpace(c.Name) &&
            !string.IsNullOrWhiteSpace(c.EnglishName) &&
            !string.IsNullOrWhiteSpace(c.Slug) &&
            !string.IsNullOrWhiteSpace(c.Icon) &&
            !string.IsNullOrWhiteSpace(c.Color));
    }

    [Fact]
    public async Task Category_ids_match_the_ServiceCategory_enum()
    {
        // The id is the shared contract with the frontend enum and the database column.
        var response = await GetAsync<List<CategoryWithCountViewModel>>("/api/v1/categories");

        foreach (var category in ServiceCategoryExtensions.GetAll())
        {
            var row = response.Data!.Single(c => c.Id == (int)category);
            row.Key.Should().Be(category.ToString());
            row.Slug.Should().Be(category.ToSlug());
            row.Name.Should().Be(category.ToPersianName());
        }
    }

    [Fact]
    public async Task A_category_with_providers_reports_its_count_and_is_not_coming_soon()
    {
        await SeedActiveProviderAsync(ServiceCategory.NailSalon);

        var response = await GetAsync<List<CategoryWithCountViewModel>>("/api/v1/categories");

        var nails = response.Data!.Single(c => c.Id == (int)ServiceCategory.NailSalon);
        nails.ProviderCount.Should().BeGreaterThan(0);
        nails.IsComingSoon.Should().BeFalse();
    }

    [Fact]
    public async Task A_category_with_no_providers_is_shown_and_flagged_coming_soon()
    {
        var response = await GetAsync<List<CategoryWithCountViewModel>>("/api/v1/categories");

        var empty = response.Data!.Where(c => c.ProviderCount == 0).ToList();
        empty.Should().NotBeEmpty("the seeded catalogue does not cover every category");
        empty.Should().OnlyContain(c => c.IsComingSoon);
    }

    #endregion

    #region GET /api/v1/categories/popular

    [Fact]
    public async Task Popular_categories_exclude_the_empty_ones()
    {
        await SeedActiveProviderAsync(ServiceCategory.Yoga);

        var response = await GetAsync<List<CategoryWithCountViewModel>>("/api/v1/categories/popular");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeEmpty();
        response.Data!.Should().OnlyContain(c => c.ProviderCount > 0 && !c.IsComingSoon);
    }

    [Fact]
    public async Task Popular_categories_are_ranked_by_provider_count()
    {
        await SeedActiveProviderAsync(ServiceCategory.Automotive);
        await SeedActiveProviderAsync(ServiceCategory.Automotive);
        await SeedActiveProviderAsync(ServiceCategory.Automotive);

        var response = await GetAsync<List<CategoryWithCountViewModel>>("/api/v1/categories/popular");

        var counts = response.Data!.Select(c => c.ProviderCount).ToList();
        counts.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Popular_categories_honour_the_limit()
    {
        var response = await GetAsync<List<CategoryWithCountViewModel>>("/api/v1/categories/popular?limit=3");

        response.Data!.Count.Should().BeLessThanOrEqualTo(3);
    }

    #endregion

    #region GET /api/v1/categories/{category}/providers

    [Fact]
    public async Task A_category_page_can_be_reached_by_slug()
    {
        await SeedActiveProviderAsync(ServiceCategory.Barbershop);

        var response = await GetAsync<PagedResult<ProviderSearchItem>>(
            "/api/v1/categories/barbershop/providers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data!.Items.Should().NotBeEmpty();
        response.Data.Items.Should().OnlyContain(p => p.PrimaryCategory == ServiceCategory.Barbershop);
    }

    [Fact]
    public async Task A_category_page_can_be_reached_by_numeric_id()
    {
        await SeedActiveProviderAsync(ServiceCategory.Barbershop);

        var response = await GetAsync<PagedResult<ProviderSearchItem>>(
            $"/api/v1/categories/{(int)ServiceCategory.Barbershop}/providers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data!.Items.Should().OnlyContain(p => p.PrimaryCategory == ServiceCategory.Barbershop);
    }

    [Fact]
    public async Task A_category_page_excludes_providers_from_other_categories()
    {
        await SeedActiveProviderAsync(ServiceCategory.Dental);
        await SeedActiveProviderAsync(ServiceCategory.Gym);

        var response = await GetAsync<PagedResult<ProviderSearchItem>>(
            "/api/v1/categories/dental/providers");

        response.Data!.Items.Should().NotBeEmpty();
        response.Data.Items.Should().OnlyContain(p => p.PrimaryCategory == ServiceCategory.Dental);
    }

    [Theory]
    [InlineData("not-a-category")]
    [InlineData("0")]
    [InlineData("999")]
    public async Task An_unknown_category_404s_rather_than_returning_everything(string category)
    {
        // Silently ignoring an unparsed filter is how `?ServiceCategory=Barbershop` once returned
        // the entire catalogue; the route must fail loudly instead.
        var raw = await GetAsync($"/api/v1/categories/{category}/providers");

        raw.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Provider search — the serviceCategory string filter

    // /providers/search projects to ProviderSearchResponse, which reports the category as the
    // enum member name in `type` rather than exposing the enum itself.
    [Theory]
    [InlineData("physiotherapy", ServiceCategory.Physiotherapy)]   // slug — what the filter UI sends
    [InlineData("hair-salon", ServiceCategory.HairSalon)]          // hyphenated slug
    [InlineData("Tutoring", ServiceCategory.Tutoring)]             // enum member name
    [InlineData("barber", ServiceCategory.Barbershop)]             // legacy alias
    public async Task Provider_search_resolves_every_accepted_category_form(
        string filterValue,
        ServiceCategory expected)
    {
        // Requiring the enum member name meant every slug-shaped value matched nothing, because
        // the specification deliberately fails closed on an unrecognised category.
        await SeedActiveProviderAsync(expected);

        var response = await GetAsync<PagedResult<ProviderSearchResponse>>(
            $"/api/v1/providers/search?serviceCategory={filterValue}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data!.Items.Should().NotBeEmpty();
        response.Data.Items.Should().OnlyContain(p => p.Type == expected.ToString());
    }

    [Fact]
    public async Task Provider_search_returns_nothing_for_an_unrecognised_category()
    {
        // Failing closed is deliberate: returning the whole catalogue reads as "here are your
        // results" while quietly hiding that the filter never applied.
        await SeedActiveProviderAsync(ServiceCategory.Gym);

        var response = await GetAsync<PagedResult<ProviderSearchResponse>>(
            "/api/v1/providers/search?serviceCategory=not-a-category");

        response.Data!.Items.Should().BeEmpty();
    }

    #endregion
}
